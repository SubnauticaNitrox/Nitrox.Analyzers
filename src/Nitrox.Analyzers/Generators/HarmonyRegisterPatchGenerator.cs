using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nitrox.Analyzers.Generators;

/// <summary>
///     Implements the harmony patch registry boilerplate for NitroxPatch inherited types by scanning its static MethodInfo
///     fields and static patch methods.
/// </summary>
[Generator(LanguageNames.CSharp)]
internal sealed class HarmonyRegisterPatchGenerator : IIncrementalGenerator
{
    private static readonly HashSet<string> harmonyMethodTypes = ["prefix", "postfix", "transpiler", "finalizer", "manipulator"];
    private static readonly HashSet<string> validTargetMethodNamePrefixes = ["targetmethod", "target_method"];
    private static readonly string generatedCodeAttribute = $@"[global::System.CodeDom.Compiler.GeneratedCode(""{typeof(HarmonyRegisterPatchGenerator).FullName}"", ""{typeof(HarmonyRegisterPatchGenerator).Assembly.GetName().Version}"")]";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Setup compilation pipeline for assemblies that use NitroxPatch.
        var compilationPipeline = context.CompilationProvider.Select((c, _) => c.GetType("NitroxPatcher", "NitroxPatcher.Patches.NitroxPatch") != null);
        // Look for partial types inheriting our NitroxPatch type, selecting all the harmony methods and target method infos.
        var harmonyMethodsWithTargetMethods = context.SyntaxProvider
                                                     .CreateSyntaxProvider(
                                                         static (node, _) => IsSyntaxTargetForGeneration(node),
                                                         static (context, _) => GetSemanticTargetForGeneration(context))
                                                     .Where(r => r is not null);
        // Register the pipeline into the compiler.
        var combinedPipeline = harmonyMethodsWithTargetMethods.Combine(compilationPipeline);
        context.RegisterSourceOutput(combinedPipeline, static (context, source) => Execute(context, source.Left!));
    }

    private static void Execute(SourceProductionContext context, NitroxPatchInfo patchInfo)
    {
        // Build Patch method implementation.
        StringBuilder patchImpl = new();
        for (int fieldIndex = 0; fieldIndex < patchInfo.Fields.Length; fieldIndex++)
        {
            NitroxPatchInfo.Field patchField = patchInfo.Fields[fieldIndex];
            patchImpl.Append("PatchMultiple(harmony, ")
                     .Append(patchField.Name);
            if (patchInfo.Functions.Length > 0)
            {
                patchImpl.Append(", ");
                foreach (NitroxPatchInfo.Function patchFunction in patchInfo.Functions)
                {
                    patchImpl.Append(patchFunction.HarmonyPatchTypeName)
                             .Append(": ((Delegate)")
                             .Append(patchFunction.Name)
                             .Append(").Method, ");
                }
                patchImpl.Remove(patchImpl.Length - 2, 2);
            }
            patchImpl.Append(");");
            // Append new line if not last implementation line.
            if (fieldIndex < patchInfo.Fields.Length - 1)
            {
                patchImpl.AppendLine().Append("        ");
            }
        }

        // Append new code to the compilation.
        context.AddSource($"{patchInfo.NameSpace}.{patchInfo.TypeName}.g.cs",
                          $$"""
                            #pragma warning disable
                            using System;
                            using HarmonyLib;

                            namespace {{patchInfo.NameSpace}};

                            partial class {{patchInfo.TypeName}}
                            {
                                {{generatedCodeAttribute}}
                                public override void Patch(Harmony harmony)
                                {
                                    {{patchImpl}}
                                }
                            }
                            """);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is not TypeDeclarationSyntax type)
        {
            return false;
        }
        if (!type.IsPartial())
        {
            return false;
        }
        // Skip if not deriving from "NitroxPatch".
        if (type.BaseList?.Types.FirstOrDefault(t => t.ToString().Equals("NitroxPatch", StringComparison.Ordinal)) == null)
        {
            return false;
        }
        // Skip if "Patch" method is already defined.
        if (type.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Modifiers.Any(SyntaxKind.OverrideKeyword) && m.Identifier.ValueText == "Patch"))
        {
            return false;
        }
        return true;
    }

    private static NitroxPatchInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        static bool IsValidPatchMethodName(string methodName, out string? patchTypeName)
        {
            foreach (string harmonyMethodType in harmonyMethodTypes)
            {
                if (methodName.StartsWith(harmonyMethodType, StringComparison.OrdinalIgnoreCase))
                {
                    patchTypeName = harmonyMethodType;
                    return true;
                }
            }
            patchTypeName = null;
            return false;
        }

        static bool IsValidTargetMethodFieldName(string fieldName, out string? targetMethodType)
        {
            foreach (string validTargetMethodName in validTargetMethodNamePrefixes)
            {
                if (fieldName.StartsWith(validTargetMethodName, StringComparison.OrdinalIgnoreCase))
                {
                    targetMethodType = validTargetMethodName;
                    return true;
                }
            }
            targetMethodType = null;
            return false;
        }

        if (context.Node is not TypeDeclarationSyntax type)
        {
            return null;
        }
        string? namespaceName = type.GetNamespaceName();
        if (namespaceName == null)
        {
            return null;
        }
        var members = type.Members.ToImmutableArray();
        return new NitroxPatchInfo(namespaceName,
                                   type.Identifier.ValueText,
                                   members.OfType<MethodDeclarationSyntax>()
                                          .Where(m => m.Modifiers.Any(SyntaxKind.StaticKeyword) && IsValidPatchMethodName(m.Identifier.ValueText, out string _))
                                          .Select(m =>
                                          {
                                              IsValidPatchMethodName(m.Identifier.ValueText, out string? patchTypeName);
                                              if (patchTypeName == null)
                                              {
                                                  return null;
                                              }
                                              return new NitroxPatchInfo.Function(m.Identifier.ValueText, patchTypeName);
                                          })
                                          .Where(m => m != null)
                                          .ToImmutableArray()!,
                                   members.OfType<FieldDeclarationSyntax>()
                                          .Where(m => m.Modifiers.Any(SyntaxKind.StaticKeyword) && m.GetReturnTypeName() == nameof(MethodInfo) && IsValidTargetMethodFieldName(m.GetName(), out string _))
                                          .Select(m =>
                                          {
                                              IsValidTargetMethodFieldName(m.GetName(), out string? targetMethodType);
                                              if (targetMethodType == null)
                                              {
                                                  return null;
                                              }
                                              return new NitroxPatchInfo.Field(m.GetName(), targetMethodType);
                                          })
                                          .Where(m => m != null)
                                          .ToImmutableArray()!);
    }

    /// <param name="NameSpace">Namespace that the patch is in.</param>
    /// <param name="TypeName">Name of the patch type.</param>
    /// <param name="Functions">Harmony patch functions declared in the patch.</param>
    /// <param name="Fields">Fields that specify a method info to be patched.</param>
    private record NitroxPatchInfo(string NameSpace, string TypeName, ImmutableArray<NitroxPatchInfo.Function> Functions, ImmutableArray<NitroxPatchInfo.Field> Fields)
    {
        public virtual bool Equals(NitroxPatchInfo? other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return NameSpace == other.NameSpace && TypeName == other.TypeName && Functions.SequenceEqual(other.Functions) && Fields.SequenceEqual(other.Fields);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = NameSpace.GetHashCode();
                hashCode = hashCode * 397 ^ TypeName.GetHashCode();
                hashCode = hashCode * 397 ^ Functions.GetHashCode();
                hashCode = hashCode * 397 ^ Fields.GetHashCode();
                return hashCode;
            }
        }

        public record Field(string Name, string TargetMethodTypeName);

        public record Function(string Name, string HarmonyPatchTypeName);
    }
}
