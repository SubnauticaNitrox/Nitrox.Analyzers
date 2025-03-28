﻿using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nitrox.Analyzers.Diagnostics;

/// <summary>
///     Test that calls, returning an IEnumerator, are iterated (i.e. MoveNext is called).
///     If they aren't iterated then the code in them won't continue after the first 'yield return'.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumeratorUsageAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rules.UnusedEnumerator];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(static c => AnalyzeIEnumeratorInvocation(c), SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeIEnumeratorInvocation(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax expression = (InvocationExpressionSyntax)context.Node;
        if (expression.Parent == null)
        {
            return;
        }
        if (context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }
        // Ignore if method invoke is used/wrapped by something (variable declaration, as a parameter, etc).
        if (!expression.Parent.IsKind(SyntaxKind.ExpressionStatement))
        {
            return;
        }
        if (!methodSymbol.ReturnType.IsType(context.SemanticModel, "System.Collections.IEnumerator"))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.UnusedEnumerator, expression.GetLocation(), methodSymbol.Name));
    }

    public static class Rules
    {
        private const string AnalyzerId = "NEU"; // Nitrox Enumerator Usage
        public const string UnusedEnumeratorDiagnosticId = $"{AnalyzerId}001";

        internal static readonly DiagnosticDescriptor UnusedEnumerator = new(UnusedEnumeratorDiagnosticId,
                                                                             "IEnumerator is not iterated",
                                                                             $"The IEnumerator '{{0}}' must be iterated by calling its {nameof(IEnumerator.MoveNext)} otherwise it will stop executing at the first 'yield return' expression",
                                                                             "Usage",
                                                                             DiagnosticSeverity.Warning,
                                                                             true);
    }
}
