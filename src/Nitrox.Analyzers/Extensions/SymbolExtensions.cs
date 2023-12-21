using Microsoft.CodeAnalysis;

namespace Nitrox.Analyzers.Extensions;

internal static class SymbolExtensions
{
    public static bool IsType(this ITypeSymbol symbol, SemanticModel semanticModel, string fullyQualifiedTypeName)
    {
        INamedTypeSymbol? namedTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
        return namedTypeSymbol != null && symbol.IsType(namedTypeSymbol);
    }

    public static bool IsType(this ITypeSymbol symbol, ITypeSymbol targetingSymbol)
    {
        if (SymbolEqualityComparer.Default.Equals(symbol, targetingSymbol))
        {
            return true;
        }
        while (symbol.BaseType is { } baseTypeSymbol)
        {
            symbol = baseTypeSymbol;
            if (SymbolEqualityComparer.Default.Equals(symbol, targetingSymbol))
            {
                return true;
            }
        }
        return false;
    }
}
