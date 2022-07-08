using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nitrox.Analyzers;

/// <summary>
///     Test code that every class which inherits from "Packet" should have a public parameterless constructor. This requirement is needed for some deserializers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DeserializablePacketAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor rule = new(ANALYZER_ID, "Packet must be deserializable", "Packet class {0} does not have a public, parameterless constructor",
        "Usage", DiagnosticSeverity.Error, true, "Tests that Nitrox packet types have valid deserialization constructor that has no parameters.");

    private const string ANALYZER_ID = nameof(DeserializablePacketAnalyzer);
    private const string PACKET_CLASS_NAME = "Packet";

    /// <summary>
    ///     Gets the list of rules of supported diagnostics.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

    /// <summary>
    ///     Initializes the analyzer by registering on symbol occurrence in the targeted code.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
    {
        INamedTypeSymbol typeSymbol = (INamedTypeSymbol)context.Symbol;
        if (!string.Equals(PACKET_CLASS_NAME, typeSymbol.BaseType?.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (typeSymbol.InstanceConstructors.Any(ctor => ctor.DeclaredAccessibility == Accessibility.Public && ctor.Parameters.Length < 1))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(rule, typeSymbol.Locations[0], typeSymbol.Name));
    }
}