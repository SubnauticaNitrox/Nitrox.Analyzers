using Microsoft.CodeAnalysis;
using Nitrox.Analyzers.Diagnostics;
using Nitrox.Analyzers.Models;

namespace Nitrox.Analyzers.Generators;

[Generator(LanguageNames.CSharp)]
internal sealed class UnitySkippedObjectLifetimeHelperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Setup compilation pipeline for assemblies that use UnityEngine.
        var compilationPipeline = context.CompilationProvider.Select((c, _) => c.GetType("UnityEngine.CoreModule", "UnityEngine.Object") != null);

        // Register the pipeline into the compiler.
        context.RegisterSourceOutput(compilationPipeline, static (context, hasUnityObject) => Execute(context, hasUnityObject));
    }

    private static void Execute(SourceProductionContext sourceProductionContext, bool hasUnityObject)
    {
        if (!hasUnityObject)
        {
            return;
        }

        sourceProductionContext.AddSource($"{UnitySkippedObjectLifetimeAnalyzer.FixFunctionName}Extension.g.cs",
                                          $$"""
                                            {{Constants.GeneratedFileHeader}}
                                            internal static class {{UnitySkippedObjectLifetimeAnalyzer.FixFunctionName}}Extension
                                            {
                                                /// <summary>
                                                ///     Returns null if Unity has marked this object as dead.
                                                /// </summary>
                                                /// <param name="obj">Unity <see cref="UnityEngine.Object" /> to check if alive.</param>
                                                /// <typeparam name="TObject">Type of Unity object that can be marked as either alive or dead.</typeparam>
                                                /// <returns>The <see cref="UnityEngine.Object" /> if alive or null if dead.</returns>
                                                {{Constants.GeneratedCodeAttribute}}
                                                public static TObject {{UnitySkippedObjectLifetimeAnalyzer.FixFunctionName}}<TObject>(this TObject obj) where TObject : UnityEngine.Object
                                                {
                                                    if (obj)
                                                    {
                                                        return obj;
                                                    }
                                            
                                                    return null;
                                                }
                                            }
                                            """);
    }
}
