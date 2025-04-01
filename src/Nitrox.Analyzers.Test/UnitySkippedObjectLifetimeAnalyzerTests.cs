using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Nitrox.Analyzers.Diagnostics;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.AnalyzerVerifier<Nitrox.Analyzers.Diagnostics.UnitySkippedObjectLifetimeAnalyzer>;
using Rules = Nitrox.Analyzers.Diagnostics.UnitySkippedObjectLifetimeAnalyzer.Rules;

namespace Nitrox.Analyzers.Test;

[TestClass]
public class UnitySkippedObjectLifetimeAnalyzerTests
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
            $$"""
            using System;

            namespace UnityEngine
            {
                public class Object
                {
                    public int Id;
                    
                    public bool {{UnitySkippedObjectLifetimeAnalyzer.FixFunctionName}}()
                    {
                        return true;
                    }
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
                    Console.WriteLine(obj is {} nonNullObj);
                    Console.WriteLine(obj.{{UnitySkippedObjectLifetimeAnalyzer.FixFunctionName}}() is {} nonNullObj2);
                }
            }
            """,
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(22, 27, 22, 42).WithArguments("Object"),
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(21, 27, 21, 38).WithArguments("Object"),
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(23, 27, 23, 36).WithArguments("Object"),
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(24, 27, 24, 40).WithArguments("Object"),
            Verify.Diagnostic(Rules.IsNullDiagnosticId).WithSpan(25, 27, 25, 47).WithArguments("Object")
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
