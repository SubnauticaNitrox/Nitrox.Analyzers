using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nitrox.Analyzers.Diagnostics;

/// <summary>
///     Tests that requested localization keys exist in the English localization file.
///     TODO: 1) Don't hard code; allow configuration via MSBuild properties. 2) Supply language file(s) to this analyzer via compiler pipeline instead of using IO, which is bad practice.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LocalizationAnalyzer : DiagnosticAnalyzer
{
    private const string NitroxLocalizationPrefix = "Nitrox_";
    private static readonly string RelativePathFromSolutionDirToEnglishLanguageFile = Path.Combine("Nitrox.Assets.Subnautica", "LanguageFiles", "en.json");
    private static readonly Regex LocalizationParseRegex = new("""
                                                               ^\s*"([^"]+)"\s*:\s*"([^"]+)"
                                                               """, RegexOptions.Multiline);

    /// <summary>
    ///     Gets the list of rules of supported diagnostics.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rules.InvalidLocalizationKeyRule);

    /// <summary>
    ///     Initializes the analyzer by registering on symbol occurrence in the targeted code.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(startContext =>
        {
            IMethodSymbol? languageGetMethodSymbol = startContext.Compilation.GetTypesByMetadataName("Language").FirstOrDefault(a => a.ContainingAssembly.Name.Equals("Assembly-Csharp", StringComparison.OrdinalIgnoreCase))?.GetMembers("Get").FirstOrDefault(m => m.Kind == SymbolKind.Method) as IMethodSymbol;
            if (languageGetMethodSymbol == null)
            {
                return;
            }

            startContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out string? projectDir);
            if (LocalizationHelper.TryLoad(projectDir))
            {
                startContext.RegisterSyntaxNodeAction(c => AnalyzeStringNode(c, languageGetMethodSymbol), SyntaxKind.StringLiteralExpression);
                startContext.RegisterSyntaxNodeAction(AnalyzeSetTextStringNode, SyntaxKind.InvocationExpression);
            }
        });
    }

    /// <summary>
    ///     Analyzes string literals in code that are passed as argument to 'Language.main.Get'.
    /// </summary>
    private void AnalyzeStringNode(SyntaxNodeAnalysisContext context, IMethodSymbol languageGetMethodSymbol)
    {
        LiteralExpressionSyntax expression = (LiteralExpressionSyntax)context.Node;
        if (expression.Parent is not ArgumentSyntax argument)
        {
            return;
        }
        if (argument.Parent is not { Parent: InvocationExpressionSyntax invocation })
        {
            return;
        }
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return;
        }
        if (!SymbolEqualityComparer.Default.Equals(method, languageGetMethodSymbol))
        {
            return;
        }
        // Ignore language call for non-nitrox keys.
        string stringValue = expression.Token.ValueText;
        if (!stringValue.StartsWith(NitroxLocalizationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (LocalizationHelper.ContainsKey(stringValue))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rules.InvalidLocalizationKeyRule, context.Node.GetLocation(), stringValue, LocalizationHelper.FileName));
    }


    /// <summary>
    ///     Analyzes SetText parameter literals in code.
    /// </summary>
    private void AnalyzeSetTextStringNode(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax expression = (InvocationExpressionSyntax)context.Node;
        if (expression.ChildNodes().FirstOrDefault(n => n is MemberAccessExpressionSyntax) is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return;
        }
        // Performance: check SetText first to not call SemanticModel lookup for every invocation expression.
        IdentifierNameSyntax? funcName = memberAccessExpression.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault();
        if (funcName?.Identifier.ValueText != "SetText")
        {
            return;
        }

        // "translate" is by default false, only interesting if "translate" != false
        if (expression.ArgumentList.Arguments.Count != 2)
        {
            return;
        }
        SyntaxNode? secondArgumentNode = expression.ArgumentList.Arguments.ElementAtOrDefault(1)?.ChildNodes().FirstOrDefault();
        if (secondArgumentNode is LiteralExpressionSyntax literalSecond && literalSecond.IsKind(SyntaxKind.FalseLiteralExpression))
        {
            return;
        }
        SyntaxNode? firstArgumentNode = expression.ArgumentList.Arguments.ElementAtOrDefault(0)?.ChildNodes().FirstOrDefault();
        if (firstArgumentNode is not LiteralExpressionSyntax literalFirst)
        {
            return;
        }

        // Ignore language call for non-nitrox keys.
        string stringValue = literalFirst.Token.ValueText;
        if (!stringValue.StartsWith(NitroxLocalizationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        
        // Now run the expensive code, checking if it's a call on a uGUI_Text method. 
        if (context.SemanticModel.GetSymbolInfo(expression).Symbol?.ContainingType.Name != "uGUI_Text")
        {
            return;
        }
        if (LocalizationHelper.ContainsKey(stringValue))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rules.InvalidLocalizationKeyRule, context.Node.GetLocation(), stringValue, LocalizationHelper.FileName));
    }

    private static class Rules
    {
        private const string AnalyzerId = "NXLZ"; // Nitrox Localization
        private const string InvalidLocalizationKeyDiagnosticId = $"{AnalyzerId}001";

        public static readonly DiagnosticDescriptor InvalidLocalizationKeyRule = new(InvalidLocalizationKeyDiagnosticId,
                                                                                     "Tests localization usages are valid",
                                                                                     "Localization key '{0}' does not exist in '{1}'",
                                                                                     "Usage",
                                                                                     DiagnosticSeverity.Warning,
                                                                                     true,
                                                                                     "Tests that requested localization keys exist in the English localization file");
    }
    
    /// <summary>
    ///     Wrapper API for synchronized access to the English localization file.
    /// </summary>
    private static class LocalizationHelper
    {
        private static readonly object locker = new();
        private static string EnglishLocalizationFileName { get; set; } = "";
        private static ImmutableDictionary<string, string> EnglishLocalization { get; set; } = ImmutableDictionary<string, string>.Empty;

        public static bool IsEmpty
        {
            get
            {
                lock (locker)
                {
                    return EnglishLocalization.IsEmpty;
                }
            }
        }

        public static string FileName
        {
            get
            {
                lock (locker)
                {
                    return EnglishLocalizationFileName;
                }
            }
        }

        public static bool ContainsKey(string key)
        {
            lock (locker)
            {
                return EnglishLocalization.ContainsKey(key);
            }
        }

        public static bool TryLoad(string? projectDir)
        {
            if (string.IsNullOrWhiteSpace(projectDir))
            {
                return false;
            }
            string? solutionDir = Directory.GetParent(projectDir)?.Parent?.FullName;
            if (!Directory.Exists(solutionDir))
            {
                return false;
            }

            string enJson;
            lock (locker)
            {
                EnglishLocalizationFileName = Path.Combine(solutionDir, RelativePathFromSolutionDirToEnglishLanguageFile);
                if (!File.Exists(EnglishLocalizationFileName))
                {
                    return false;
                }

                enJson = File.ReadAllText(EnglishLocalizationFileName);
            }
            // Parse localization JSON to dictionary for lookup.
            Dictionary<string, string> keyValue = [];
            foreach (Match match in LocalizationParseRegex.Matches(enJson))
            {
                keyValue.Add(match.Groups[1].Value, match.Groups[2].Value);
            }
            lock (locker)
            {
                EnglishLocalization = keyValue.ToImmutableDictionary();
            }
            return true;
        }
    }
}
