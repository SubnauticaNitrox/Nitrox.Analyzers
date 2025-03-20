using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nitrox.Analyzers.Helpers;
using Nitrox.Analyzers.Models;

namespace Nitrox.Analyzers.Extensions;

internal static class SyntaxExtensions
{
    public static bool IsPartial(this TypeDeclarationSyntax typeSyntax) => typeSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

    public static bool IsAbstract(this TypeDeclarationSyntax typeSyntax) => typeSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword);

    public static bool HasAttributeWithName(this TypeDeclarationSyntax typeSyntax, string attributeName)
    {
        foreach (AttributeListSyntax attributeList in typeSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                if (attribute.GetName() == attributeName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool HasAttribute(this TypeDeclarationSyntax typeSyntax, SemanticModel semanticModel, INamedTypeSymbol attr)
    {
        foreach (AttributeListSyntax attributeList in typeSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(attribute).Type, attr))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string? GetNamespaceName(this TypeDeclarationSyntax type)
    {
        return type.Ancestors()
                   .Select(n => n switch
                   {
                       FileScopedNamespaceDeclarationSyntax f => f.Name.ToString(),
                       NamespaceDeclarationSyntax ns => ns.Name.ToString(),
                       _ => null
                   })
                   .First();
    }

    public static string GetReturnTypeName(this MemberDeclarationSyntax member)
    {
        switch (member)
        {
            case FieldDeclarationSyntax field:
                foreach (SyntaxNode node in field.Declaration.ChildNodes())
                {
                    switch (node)
                    {
                        case IdentifierNameSyntax identifierName:
                            return identifierName.Identifier.ValueText;
                        case QualifiedNameSyntax qualifiedName:
                            return qualifiedName.Right.Identifier.ValueText;
                        default:
                            continue;
                    }
                }
                return "";
            case MethodDeclarationSyntax method:
                return method.ReturnType.ToString();
            default:
                return "";
        }
    }

    public static T? FindInAncestors<T>(this SyntaxNode node) where T : SyntaxNode
    {
        SyntaxNode? cur = node.Parent;
        while (cur is not null and not T)
        {
            cur = cur.Parent;
        }
        return (T?)cur;
    }

    public static string GetName(this SyntaxNode node)
    {
        return node switch
        {
            FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? "",
            TypeDeclarationSyntax type => type.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => node.TryGetInferredMemberName() ?? ""
        };
    }

    public static EquatableArray<MethodParameterInfo> GetParameterInfos(this IMethodSymbol symbol)
    {
        if (symbol.Parameters.IsDefaultOrEmpty)
        {
            return [];
        }
        return symbol.Parameters.Select((p, index) => new MethodParameterInfo(p.Name == "" ? $"p{index}" : p.Name, p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))).ToImmutableArray();
    }

    public static InterceptableCall? TryGetInterceptableCall(this SyntaxNode node, GeneratorSyntaxContext context)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return null;
        }
        if (node.TryGetCallLocation(context) is not { } callLocation)
        {
            return null;
        }
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }
        string receiverType = methodSymbol.ReceiverType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "";
        return new InterceptableCall(callLocation, receiverType, methodSymbol.GetParameterInfos(), methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), methodSymbol.IsAsync, methodSymbol.IsStatic, methodSymbol.Name,
                                     methodSymbol.GetGeneratorSafeNamespace(), methodSymbol.DeclaredAccessibility);
    }

    public static CallLocation? TryGetCallLocation(this SyntaxNode node, GeneratorSyntaxContext context)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return null;
        }
        InterceptableLocation? interceptableLocation = context.SemanticModel.GetInterceptableLocation(invocation);
        if (interceptableLocation is null)
        {
            return null;
        }
        return GetCallLocation(interceptableLocation, invocation);

        static CallLocation? GetCallLocation(InterceptableLocation interceptLocation, SyntaxNode node)
        {
            Location? location = null;
            while (location == null)
            {
                switch (node)
                {
                    case InvocationExpressionSyntax { Expression: IdentifierNameSyntax } invocation:
                        location = invocation.GetLocation();
                        break;
                    case InvocationExpressionSyntax invocation:
                        node = invocation.Expression;
                        break;
                    case MemberAccessExpressionSyntax memberAccess:
                        location = memberAccess.GetLocation();
                        break;
                    default:
                        return null;
                }
            }

            FileLinePositionSpan lineSpan = location.GetLineSpan();
            return new CallLocation(lineSpan.Path,
                                    lineSpan.StartLinePosition.Line + 1,
                                    lineSpan.Span.Start.Character + 1,
                                    interceptLocation.GetInterceptsLocationAttributeSyntax()
            );
        }
    }
}
