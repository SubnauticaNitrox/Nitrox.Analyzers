using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nitrox.Analyzers.Diagnostics;

/// <summary>
///     Test that Unity objects are properly checked for their lifetime.
///     The lifetime check is skipped when using "is null" or "obj?.member" as opposed to "== null".
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnitySkippedObjectLifetimeAnalyzer : DiagnosticAnalyzer
{
    public const string FixFunctionName = "AliveOrNull";

    /// <summary>
    ///     Gets the list of rules of supported diagnostics.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rules.ConditionalAccessRule, Rules.IsNullRule, Rules.NullCoalesceRule];

    /// <summary>
    ///     Initializes the analyzer by registering on symbol occurrence in the targeted code.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compStartContext =>
        {
            INamedTypeSymbol? unityObjectTypeSymbol = compStartContext.Compilation.GetTypeByMetadataName("UnityEngine.Object");
            if (unityObjectTypeSymbol == null)
            {
                return;
            }

            compStartContext.RegisterSyntaxNodeAction(c => AnalyzeIsNullNode(c, unityObjectTypeSymbol), SyntaxKind.IsPatternExpression);
            compStartContext.RegisterSyntaxNodeAction(c => AnalyzeConditionalAccessNode(c, unityObjectTypeSymbol), SyntaxKind.ConditionalAccessExpression);
            compStartContext.RegisterSyntaxNodeAction(c => AnalyzeCoalesceNode(c, unityObjectTypeSymbol), SyntaxKind.CoalesceExpression);
        });
    }

    private void AnalyzeIsNullNode(SyntaxNodeAnalysisContext context, ITypeSymbol unityObjectSymbol)
    {
        IsPatternExpressionSyntax expression = (IsPatternExpressionSyntax)context.Node;

        // Is this a null check pattern (as opposed to type match pattern)?
        if (!IsNullPattern(expression.Pattern))
        {
            return;
        }
        // Is it on a UnityEngine.Object?
        if (!IsUnityObjectExpression(context, expression.Expression, unityObjectSymbol, out ITypeSymbol? originSymbol))
        {
            return;
        }
        // ... and not handled by "IsAliveOrNull"?
        if (expression.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } && member.GetName() == FixFunctionName)
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rules.IsNullRule, expression.GetLocation(), originSymbol!.Name));

        static bool IsNullPattern(PatternSyntax patternSyntax)
        {
            return patternSyntax switch
            {
                UnaryPatternSyntax { Pattern: ConstantPatternSyntax constant } => IsNullPattern(constant),
                UnaryPatternSyntax { Pattern: RecursivePatternSyntax recursive } => IsNullPattern(recursive),
                ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } when literal.IsKind(SyntaxKind.NullLiteralExpression) => true,
                RecursivePatternSyntax { PropertyPatternClause.Subpatterns.Count: 0 } => true,
                _ => false
            };
        }
    }

    private void AnalyzeConditionalAccessNode(SyntaxNodeAnalysisContext context, ITypeSymbol unityObjectSymbol)
    {
        ConditionalAccessExpressionSyntax expression = (ConditionalAccessExpressionSyntax)context.Node;
        if (IsUnityObjectExpression(context, expression.Expression, unityObjectSymbol, out ITypeSymbol? originSymbol) && !IsFixedWithAliveOrNull(context, expression))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.ConditionalAccessRule, context.Node.GetLocation(), originSymbol!.Name));
        }

        static bool IsFixedWithAliveOrNull(SyntaxNodeAnalysisContext context, ConditionalAccessExpressionSyntax expression)
        {
            return context.SemanticModel.GetSymbolInfo(expression.Expression).Symbol is IMethodSymbol { Name: FixFunctionName };
        }
    }

    private void AnalyzeCoalesceNode(SyntaxNodeAnalysisContext context, ITypeSymbol unityObjectSymbol)
    {
        BinaryExpressionSyntax expression = (BinaryExpressionSyntax)context.Node;
        if (IsUnityObjectExpression(context, expression.Left, unityObjectSymbol, out ITypeSymbol? originSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.NullCoalesceRule, context.Node.GetLocation(), originSymbol!.Name));
        }
    }

    private bool IsUnityObjectExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax possibleUnityAccessExpression, ITypeSymbol compareSymbol, out ITypeSymbol? possibleUnitySymbol)
    {
        if (possibleUnityAccessExpression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member })
        {
            possibleUnityAccessExpression = member.Expression;
        }
        possibleUnitySymbol = context.SemanticModel.GetTypeInfo(possibleUnityAccessExpression).Type;
        return possibleUnitySymbol?.IsType(compareSymbol) ?? false;
    }

    public static class Rules
    {
        private const string AnalyzerId = "NUSL"; // Nitrox Unity Skipped Lifetime
        public const string ConditionalAccessDiagnosticId = $"{AnalyzerId}001";
        public const string IsNullDiagnosticId = $"{AnalyzerId}002";
        public const string NullCoalesceDiagnosticId = $"{AnalyzerId}003";
        private const string RuleTitle = "Tests that Unity object lifetime is not ignored";
        private const string RuleDescription = "Tests that Unity object lifetime checks are not ignored.";

        internal static readonly DiagnosticDescriptor ConditionalAccessRule = new(ConditionalAccessDiagnosticId,
                                                                                  RuleTitle,
                                                                                  "'?.' is invalid on type '{0}' as it derives from 'UnityEngine.Object', bypassing the Unity object lifetime check",
                                                                                  "Usage",
                                                                                  DiagnosticSeverity.Error,
                                                                                  true,
                                                                                  RuleDescription);

        internal static readonly DiagnosticDescriptor IsNullRule = new(IsNullDiagnosticId,
                                                                       RuleTitle,
                                                                       "'is null' is invalid on type '{0}' as it derives from 'UnityEngine.Object', bypassing the Unity object lifetime check",
                                                                       "Usage",
                                                                       DiagnosticSeverity.Error,
                                                                       true,
                                                                       RuleDescription);

        internal static readonly DiagnosticDescriptor NullCoalesceRule = new(NullCoalesceDiagnosticId,
                                                                             RuleTitle,
                                                                             "'??' is invalid on type '{0}' as it derives from 'UnityEngine.Object', bypassing the Unity object lifetime check",
                                                                             "Usage",
                                                                             DiagnosticSeverity.Error,
                                                                             true,
                                                                             RuleDescription);
    }
}
