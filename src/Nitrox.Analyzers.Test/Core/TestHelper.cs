using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nitrox.Analyzers.Test.Core;

public static class TestHelper
{
    public static SyntaxTree RunGenerator<TGenerator>(string csharpCode, string expectedGeneratedFileName = "") where TGenerator : IIncrementalGenerator, new()
    {
        var driver = CSharpGeneratorDriver.Create(new TGenerator());
        var compilation = CSharpCompilation.Create(Guid.NewGuid().ToString("N"),
                                                   [CSharpSyntaxTree.ParseText(csharpCode)],
                                                   [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
        if (string.IsNullOrWhiteSpace(expectedGeneratedFileName))
        {
            return result.GeneratedTrees.Single();
        }
        return result.GeneratedTrees.Single(t => t.FilePath.EndsWith(expectedGeneratedFileName));
    }
}
