using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nitrox.Analyzers.Diagnostics;

/// <summary>
///     Dependency injection shouldn't be used in types that we can instantiate ourselves (i.e. not MonoBehaviours or
///     Harmony patches).
///     We should use the Dependency Injection container only when we can apply the DI pattern to effect. If not, making
///     said type static is often more readable and performant.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DependencyInjectionMisuseAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rules.MisusedDependencyInjection];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(analysisContext =>
        {
            INamedTypeSymbol? unityEngineObjectTypeSymbol = analysisContext.Compilation.GetTypeByMetadataName("UnityEngine.Object");
            INamedTypeSymbol? nitroxPatchTypeSymbol = analysisContext.Compilation.GetTypeByMetadataName("NitroxPatcher.Patches.NitroxPatch");

            analysisContext.RegisterSyntaxNodeAction(c => AnalyzeDependencyInjectionMisuse(c, unityEngineObjectTypeSymbol, nitroxPatchTypeSymbol), SyntaxKind.SimpleMemberAccessExpression);
        });
    }

    private static void AnalyzeDependencyInjectionMisuse(SyntaxNodeAnalysisContext context, params INamedTypeSymbol?[] allowedTypesUsingDependencyInjection)
    {
        MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)context.Node;
        if (memberAccess.Expression is not IdentifierNameSyntax accessedIdentifier)
        {
            return;
        }
        if (accessedIdentifier.GetName() != "NitroxServiceLocator")
        {
            return;
        }
        TypeDeclarationSyntax? declaringType = memberAccess.FindInAncestors<TypeDeclarationSyntax>();
        if (declaringType == null)
        {
            return;
        }
        INamedTypeSymbol? declaringTypeSymbol = context.SemanticModel.GetDeclaredSymbol(declaringType);
        if (declaringTypeSymbol == null)
        {
            return;
        }
        foreach (INamedTypeSymbol? allowedType in allowedTypesUsingDependencyInjection)
        {
            if (allowedType == null)
            {
                continue;
            }
            if (declaringTypeSymbol.IsType(allowedType))
            {
                return;
            }
        }

        Rules.ReportMisusedDependencyInjection(context, declaringType.GetName(), memberAccess.GetLocation());
    }

    private static class Rules
    {
        public static readonly DiagnosticDescriptor MisusedDependencyInjection = new("DIMA001",
                                                                                     "Dependency Injection container is used directly",
                                                                                     "The DI container should not be used directly in type '{0}' as the requested service can be supplied via a constructor parameter",
                                                                                     "Usage",
                                                                                     DiagnosticSeverity.Warning,
                                                                                     true);

        public static void ReportMisusedDependencyInjection(SyntaxNodeAnalysisContext context, string declaringTypeName, Location location)
        {
            context.ReportDiagnostic(Diagnostic.Create(MisusedDependencyInjection, location, declaringTypeName));
        }
    }
}
