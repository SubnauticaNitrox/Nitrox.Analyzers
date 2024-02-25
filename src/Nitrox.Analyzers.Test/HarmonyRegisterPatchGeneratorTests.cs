using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nitrox.Analyzers.Generators;

namespace Nitrox.Analyzers.Test;

[TestClass]
public class HarmonyRegisterPatchGeneratorTests
{
    [TestMethod]
    public void ShouldGeneratePatchForSingleTargetMethod()
    {
        const string patchClassText = @"""
namespace NitroxPatcher.Patches
{
    public abstract class NitroxPatch
    {
        public abstract void Patch(Harmony harmony);
    }
}

namespace TestNamespace
{
    public sealed partial class SomePatch : NitroxPatch
    {
        public static System.Reflection.MethodInfo TARGET_METHOD = ((Delegate)File.Create).Method;

        public static bool Prefix()
        {
            return false;
        }
    }
}
""";
        string expectedGeneratedCode = $$"""
#pragma warning disable
using System;
using HarmonyLib;

namespace TestNamespace;

partial class SomePatch
{
    [global::System.CodeDom.Compiler.GeneratedCode("Nitrox.Analyzers.Generators.HarmonyRegisterPatchGenerator", "{{typeof(HarmonyRegisterPatchGenerator).Assembly.GetName().Version}}")]
    public override void Patch(Harmony harmony)
    {
        PatchMultiple(harmony, TARGET_METHOD, prefix: ((Delegate)Prefix).Method);
    }
}
""";


        var driver = CSharpGeneratorDriver.Create(new HarmonyRegisterPatchGenerator());
        var compilation = CSharpCompilation.Create("NitroxPatcher",
                                                   new[] { CSharpSyntaxTree.ParseText(patchClassText) },
                                                   new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
        SyntaxTree generatedFileSyntax = result.GeneratedTrees.Single(t => t.FilePath.EndsWith("SomePatch.g.cs"));

        generatedFileSyntax.GetText().ToString().Should().Be(expectedGeneratedCode);
    }
}
