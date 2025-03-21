﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Nitrox.Analyzers.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Nitrox.Analyzers.Fixers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnitySkippedObjectLifetimeFixProvider))]
public sealed class UnitySkippedObjectLifetimeFixProvider : CodeFixProvider
{
    private static readonly IdentifierNameSyntax aliveOrNull = IdentifierName(UnitySkippedObjectLifetimeAnalyzer.FixFunctionName);
    public override ImmutableArray<string> FixableDiagnosticIds => [UnitySkippedObjectLifetimeAnalyzer.Rules.ConditionalAccessDiagnosticId];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // Code template from: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
        ConditionalAccessExpressionSyntax declaration = root!.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf()
                                                             .OfType<ConditionalAccessExpressionSyntax>()
                                                             .First();
        context.RegisterCodeFix(
            CodeAction.Create(
                equivalenceKey: UnitySkippedObjectLifetimeAnalyzer.Rules.ConditionalAccessDiagnosticId,
                title: "Insert AliveOrNull() before conditional access of UnityEngine.Object",
                createChangedDocument: c => InsertAliveOrNullAsync(context.Document, declaration, c)
            ),
            diagnostic);
    }

    private async Task<Document> InsertAliveOrNullAsync(Document document, ConditionalAccessExpressionSyntax declaration, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
        {
            return document;
        }

        // Wrap expression with an invocation to AliveOrNull, this will cause AliveOrNull to be called before the conditional access.
        InvocationExpressionSyntax wrappedExpression = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, declaration.Expression, aliveOrNull));
        SyntaxNode newDeclaration = declaration.ReplaceNode(declaration.Expression, wrappedExpression);
        root = root.ReplaceNode(declaration, newDeclaration);

        // Replace the old document with the new.
        return document.WithSyntaxRoot(root);
    }
}
