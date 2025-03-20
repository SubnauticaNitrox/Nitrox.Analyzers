using FluentAssertions;
using Microsoft.CodeAnalysis;
using Nitrox.Analyzers.Generators;
using Nitrox.Analyzers.Test.Core;

namespace Nitrox.Analyzers.Test;

[TestClass]
public class GuardGeneratorTests
{
    [TestMethod]
    public void ShouldIntercept()
    {
        const string targetText = @"""
internal static class Program
{
    public static void Main(string[] args)
    {
        new SomeClass().GetPlatformByGameDir(""bla"");
    }
}

public class SomeClass
{
    public async System.Threading.Tasks.Task GetPlatformByGameDir(string path)
    {
        // Do stuff
    }
}
""";

        SyntaxTree generatedFileSyntax = TestHelper.RunGenerator<GuardGenerator>(targetText);
        generatedFileSyntax.GetText().ToString().Should().Contain("class Guardian", Exactly.Once());
    }
}
