using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Nitrox.Analyzers.Boilerplate;
using Nitrox.Analyzers.Models;

namespace Nitrox.Analyzers.Generators;

[Generator(LanguageNames.CSharp)]
internal sealed class GuardGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Construct compiler pipeline.
        var guardedMethods = context.SyntaxProvider
                                    .CreateSyntaxProvider(
                                        static (node, _) => IsSyntaxTargetForGeneration(node),
                                        static (context, _) => GetSemanticTargetForGeneration(context))
                                    .Where(call => call is not null)
                                    .Select((call, _) => call!)
                                    .Collect()
                                    .SelectMany((calls, _) => calls.GroupBy(c => c.OwnerNamespace).Select(g => (g.Key, Calls: g.ToImmutableArray())));
        var pipeline = guardedMethods;

        // Register pipeline with function targets.
        context.RegisterImplementationSourceOutput(pipeline, static (executeContext, pipelineData) => Execute(executeContext, pipelineData));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (GetMethodNameIdentifier(node) is not { } identifierName)
        {
            return false;
        }
        if (identifierName.IndexOf("GetPlatformByGameDir", StringComparison.OrdinalIgnoreCase) == -1)
        {
            return false;
        }
        return true;

        static string? GetMethodNameIdentifier(SyntaxNode node)
        {
            while (true)
            {
                switch (node)
                {
                    case InvocationExpressionSyntax { Expression: IdentifierNameSyntax identifier }:
                        return identifier.Identifier.ValueText;
                    case InvocationExpressionSyntax invocation:
                        node = invocation.Expression;
                        break;
                    case MemberAccessExpressionSyntax memberAccess:
                        return memberAccess.Name.Identifier.ValueText;
                    default:
                        return null;
                }
            }
        }
    }

    private static void Execute(SourceProductionContext context, (string Key, ImmutableArray<InterceptableCall> Calls) callsGroupedByNamespace)
    {
        if (callsGroupedByNamespace.Calls.IsDefaultOrEmpty || context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        context.AddSource($"{callsGroupedByNamespace.Key}.g.cs", SourceText.From(
                              $$"""
                                {{Constants.GeneratedFileHeader}}
                                {{Constants.InterceptorAttribute}}
                                {{GuardianBoilerplate.Code}}
                                namespace {{Constants.InterseptorNamespace}}
                                {
                                    {{Constants.GeneratedCodeAttribute}}
                                    file static class {{$"{callsGroupedByNamespace.Key.Replace('.', '_')}_Interceptors"}}
                                    {
                                        {{GetInterceptorDeclarations(callsGroupedByNamespace.Calls, 2)}}
                                    }
                                }
                                """, Encoding.UTF8));

        static string GetInterceptorDeclarations(ImmutableArray<InterceptableCall> calls, int indents = 0)
        {
            using IndentedTextWriter writer = new(new StringWriter());

            while (indents > 0)
            {
                writer.Indent++;
                indents--;
            }
            foreach (InterceptableCall call in calls)
            {
                writer.WriteLine(Constants.EditorNotBrowsableAttribute);
                writer.WriteLine(call.Location.InterceptableLocationSyntax);
                writer.Write("public static ");
                if (call.IsAsync)
                {
                    writer.Write("async ");
                }
                writer.Write(call.ReturnTypeName);
                writer.Write($" {call.Location.FileName}_{call.Name}_{call.Location.Line}_{call.Location.Character}(");
                if (!call.IsStatic)
                {
                    writer.Write("this ");
                    writer.Write(call.OwnerTypeName);
                    writer.Write(" @this");
                }
                if (!call.Parameters.IsDefaultOrEmpty)
                {
                    bool isFirst = call.IsStatic;
                    foreach (MethodParameterInfo parameter in call.Parameters)
                    {
                        if (!isFirst)
                        {
                            writer.Write(", ");
                        }
                        isFirst = false;

                        writer.Write(parameter.TypeName);
                        writer.Write(' ');
                        writer.Write(parameter.Name);
                    }
                }
                writer.WriteLine(')');
                writer.WriteLine('{');
                writer.Indent++;
                writer.WriteLine($"if (!Guardian.IsTrustedDirectory({call.Parameters.First(p => p.Name.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0).Name}))");
                writer.WriteLine('{');
                writer.Indent++;
                writer.WriteLine("Environment.Exit(0);");
                writer.Indent--;
                writer.WriteLine('}');
                // This calls the original function at the end.
                if (call.Accessibility is Accessibility.Public or Accessibility.Internal)
                {
                    if (call.ReturnTypeName != "void")
                    {
                        writer.Write(call.IsAsync ? "await " : "return ");
                    }
                    writer.Write(call.IsStatic ? call.OwnerTypeName : "@this");
                    writer.Write('.');
                    writer.Write(call.Name);
                    writer.Write('(');
                    if (!call.Parameters.IsDefaultOrEmpty)
                    {
                        bool isFirst = true;
                        foreach (MethodParameterInfo parameter in call.Parameters)
                        {
                            if (!isFirst)
                            {
                                writer.Write(", ");
                            }
                            isFirst = false;

                            writer.Write(parameter.Name);
                        }
                    }
                    writer.WriteLine(");");
                }
                writer.Indent--;
                writer.WriteLine('}');
                writer.WriteLine();
            }

            return writer.InnerWriter.ToString()!;
        }
    }

    private static InterceptableCall? GetSemanticTargetForGeneration(GeneratorSyntaxContext context) => context.Node.TryGetInterceptableCall(context);
}
