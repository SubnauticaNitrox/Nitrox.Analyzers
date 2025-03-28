﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nitrox.Analyzers.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringUsageAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rules.PreferInterpolatedStringRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeAddNode, SyntaxKind.AddExpression);
    }

    private void AnalyzeAddNode(SyntaxNodeAnalysisContext context)
    {
        bool IsPartOfStringConcat(SyntaxNode node)
        {
            switch (node)
            {
                case LiteralExpressionSyntax literal:
                    return literal.IsKind(SyntaxKind.StringLiteralExpression);
                case InterpolatedStringExpressionSyntax:
                    return true;
                case MemberAccessExpressionSyntax member:
                    string? memberType = context.SemanticModel.GetTypeInfo(member).ConvertedType?.Name;
                    return memberType == "String";
                case BinaryExpressionSyntax binary:
                    // If one side is string-ish then the other side will get implicitly casted to string.
                    return binary.IsKind(SyntaxKind.AddExpression) && (IsPartOfStringConcat(binary.Right) || IsPartOfStringConcat(binary.Left));
                default:
                    return false;
            }
        }

        static bool IsLeftMostNodeInConcat(SyntaxNode? node)
        {
            return node switch
            {
                BinaryExpressionSyntax => false,
                InterpolatedStringContentSyntax => false,
                ParenthesizedExpressionSyntax => IsLeftMostNodeInConcat(node.Parent),
                _ => true
            };
        }

        BinaryExpressionSyntax expression = (BinaryExpressionSyntax)context.Node;
        // Deduplicate warnings. Only left most '+' of the expression should be handled here.
        if (!IsLeftMostNodeInConcat(expression.Parent))
        {
            return;
        }
        // Test if this should be interpolated.
        if (!IsPartOfStringConcat(expression.Left) && !IsPartOfStringConcat(expression.Right))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.PreferInterpolatedStringRule, expression.GetLocation(), expression));
    }

    public static class Rules
    {
        private const string AnalyzerId = "NSU"; // Nitrox String Usage
        public const string PreferInterpolatedStringDiagnosticId = $"{AnalyzerId}001";

        internal static readonly DiagnosticDescriptor PreferInterpolatedStringRule = new(PreferInterpolatedStringDiagnosticId,
                                                                                        "Prefer interpolated string over string concat",
                                                                                        "String concat can be turned into interpolated string",
                                                                                        "Usage",
                                                                                        DiagnosticSeverity.Warning,
                                                                                        true,
                                                                                        "Prefer interpolated string over concatenating strings.");
    }
}
