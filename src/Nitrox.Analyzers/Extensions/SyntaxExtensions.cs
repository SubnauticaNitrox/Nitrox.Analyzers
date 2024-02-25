using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nitrox.Analyzers.Extensions;

internal static class SyntaxExtensions
{
    public static bool IsPartial(this TypeDeclarationSyntax typeSyntax)
    {
        return typeSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    public static bool IsAbstract(this TypeDeclarationSyntax typeSyntax)
    {
        return typeSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword);
    }

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
        // .ChildNodes().OfType<QualifiedNameSyntax>().FirstOrDefault().Right.Identifier.ValueText
        switch (member)
        {
            case FieldDeclarationSyntax field:
                string? name = field.Declaration.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault()?.Identifier.ValueText ?? field.Declaration.ChildNodes().OfType<QualifiedNameSyntax>().FirstOrDefault()?.Right.Identifier.ValueText;
                return name ?? "";
            case MethodDeclarationSyntax method:
                return method.ReturnType.ToString();
            default:
                return "";
        }
    }

    public static string GetName(this MemberDeclarationSyntax member)
    {
        return member switch
        {
            FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? "",
            TypeDeclarationSyntax type => type.Identifier.Text,
            _ => ""
        };
    }

    public static T? FindInParents<T>(this SyntaxNode node) where T : SyntaxNode
    {
        SyntaxNode? cur = node.Parent;
        while (cur is not null and not T)
        {
            cur = cur.Parent;
        }
        return (T?)cur;
    }

    public static string? GetName(this SyntaxNode node)
    {
        return node switch
        {
            FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? "",
            TypeDeclarationSyntax type => type.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => node.TryGetInferredMemberName()
        };
    }
}
