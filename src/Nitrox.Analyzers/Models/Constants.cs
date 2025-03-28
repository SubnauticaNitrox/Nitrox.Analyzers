using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Nitrox.Analyzers.Models;

internal static class Constants
{
    public const string GeneratorNamespace = "Nitrox.Analyzers";
    public const string InterseptorNamespace = $"{GeneratorNamespace}.Interceptors";
    public const string ExcludeFromCoverageAttribute = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";
    public const string DebuggerStepThroughAttribute = "[global::System.Diagnostics.DebuggerStepThrough]";
    public const string EditorNotBrowsableAttribute = "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]";

    public const string GeneratedFileHeader = """
                                              // <auto-generated/>
                                              #pragma warning disable
                                              #nullable enable annotations

                                              using System;

                                              """;

    public static readonly string GeneratedCodeAttribute = $"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{typeof(Constants).Assembly.GetName().Name}\", \"{typeof(Constants).Assembly.GetName().Version}\")]";

    /// <summary>
    ///     The source text which defines the interceptor attribute.
    ///     This should be included in any generated source file that uses interceptors.
    /// </summary>
    [field: AllowNull]
    [field: MaybeNull]
    public static string InterceptorAttribute
    {
        get
        {
            if (field != null)
            {
                return field;
            }
            using IndentedTextWriter sb = new(new StringWriter());

            sb.WriteLine("namespace System.Runtime.CompilerServices");
            sb.WriteLine('{');
            sb.Indent++;

            sb.WriteLine(GeneratedCodeAttribute);
            sb.WriteLine(EditorNotBrowsableAttribute);
            sb.WriteLine("[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]");
            sb.WriteLine("file sealed class InterceptsLocationAttribute : Attribute");
            sb.WriteLine('{');
            sb.Indent++;

            sb.WriteLine("public InterceptsLocationAttribute(int version, string data) { }");

            sb.Indent--;
            sb.WriteLine('}');

            sb.Indent--;
            sb.WriteLine('}');

            return field = sb.InnerWriter.ToString()!;
        }
    }
}
