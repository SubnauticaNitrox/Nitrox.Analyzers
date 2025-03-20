using FluentAssertions;
using Microsoft.CodeAnalysis;
using Nitrox.Analyzers.Generators;
using Nitrox.Analyzers.Models;
using Nitrox.Analyzers.Test.Core;

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
{{Constants.GeneratedFileHeader}}
namespace TestNamespace;

partial class SomePatch
{
    {{Constants.GeneratedCodeAttribute}}
    public override void Patch(HarmonyLib.Harmony harmony)
    {
        PatchMultiple(harmony, TARGET_METHOD, prefix: ((Delegate)Prefix).Method);
    }
}
""";

        SyntaxTree generatedFileSyntax = TestHelper.RunGenerator<HarmonyRegisterPatchGenerator>(patchClassText, "SomePatch.g.cs");
        generatedFileSyntax.GetText().ToString().Should().Be(expectedGeneratedCode);
    }
}
