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
        NitroxEntryPatch.Apply(""something"");
    }
}

public static class NitroxEntryPatch
{
    public static async System.Threading.Tasks.Task Apply(string path)
    {
        // Do stuff
    }
}
""";

        SyntaxTree generatedFileSyntax = TestHelper.RunGenerator<GuardGenerator>(targetText);
        generatedFileSyntax.GetText().ToString().Should().Contain("class Guardian", Exactly.Once());
    }
}
