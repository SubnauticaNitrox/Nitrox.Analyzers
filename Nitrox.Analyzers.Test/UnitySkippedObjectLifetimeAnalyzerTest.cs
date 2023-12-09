using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.AnalyzerVerifier<Nitrox.Analyzers.Diagnostics.UnitySkippedObjectLifetimeAnalyzer>;
using Rules = Nitrox.Analyzers.Diagnostics.UnitySkippedObjectLifetimeAnalyzer.Rules;

namespace Nitrox.Analyzers.Test;

[TestClass]
public class UnitySkippedObjectLifetimeAnalyzerTest
{
    [TestMethod]
    public Task ConditionalAccess_Diagnostic()
    {
        return Verify.VerifyAnalyzerAsync(
            """
            using System;

            namespace UnityEngine
            {
                public class Object
                {
                    public int Id;
                }
            }

            class Program
            {
                static void Main()
                {
                    Console.WriteLine(new UnityEngine.Object()?.Id);
                }
            }
            """,
            Verify.Diagnostic(Rules.ConditionalAccessDiagnosticId).WithSpan(15, 27, 15, 55).WithArguments("Object"));
    }

    [TestMethod]
    public Task IsNull_Diagnostic()
    {
        return Verify.VerifyAnalyzerAsync(
            """
            using System;

            namespace UnityEngine
            {
                public class Object
                {
                    public int Id;
                }
            }

            class Program
            {
                static void Main()
                {
                    UnityEngine.Object obj = new UnityEngine.Object();
                    Console.WriteLine(obj is null);
                    Console.WriteLine(obj is not null);
                    Console.WriteLine(obj is {});
                    Console.WriteLine(obj is not {});
                }
            }
            """,
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(17, 27, 17, 42).WithArguments("Object"),
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(16, 27, 16, 38).WithArguments("Object"),
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(18, 27, 18, 36).WithArguments("Object"),
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(19, 27, 19, 40).WithArguments("Object")
        );
    }

    [TestMethod]
    public Task NullCoalesce_Diagnostic()
    {
        return Verify.VerifyAnalyzerAsync(
            """
            using System;

            namespace UnityEngine
            {
                public class Object
                {
                    public int Id;
                }
            }

            class Program
            {
                static void Main()
                {
                    Console.WriteLine(new UnityEngine.Object() ?? new UnityEngine.Object());
                }
            }
            """,
            Verify.Diagnostic(Rules.NullCoalesceDiagnosticId).WithSpan(15, 27, 15, 79).WithArguments("Object")
        );
    }

    [TestMethod]
    public Task ShouldNotReportUnrelatedCode()
    {
        return Verify.VerifyAnalyzerAsync(
            """
            using System;

            namespace UnityEngine
            {
                public class Object
                {
                    public int Id;
                }
                
                public class ObjectB : Object
                {
                }
            }

            class Program
            {
                static void Main()
                {
                    if (new UnityEngine.Object() == null)
                    {
                    }
                    if (new UnityEngine.ObjectB() is UnityEngine.Object)
                    {
                    }
                    if (new UnityEngine.ObjectB() is UnityEngine.Object objB)
                    {
                    }
                }
            }
            """,
            DiagnosticResult.EmptyDiagnosticResults
        );
    }
}
