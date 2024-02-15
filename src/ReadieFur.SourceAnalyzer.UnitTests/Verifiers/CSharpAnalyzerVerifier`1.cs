using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.UnitTests.Verifiers
{
    public static partial class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
            => CSharpAnalyzerVerifier<TAnalyzer, UnitVerifier>.Diagnostic();

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<TAnalyzer, UnitVerifier>.Diagnostic(diagnosticId);

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpAnalyzerVerifier<TAnalyzer, UnitVerifier>.Diagnostic(descriptor);

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };

            //test.ReferenceAssemblies.AddAssemblies(ImmutableArray.Create(Assembly.GetExecutingAssembly().Location));

            test.ExpectedDiagnostics.AddRange(expected);

            try
            {
                await test.RunAsync(CancellationToken.None);
            }
            catch (AssertFailedException ex)
            {
                //The test will fail if the diagnostics are not a 1:1 match, we will catch this and interpret the error to determine the true result based on the diagnostic ID.

                /* Example of the message format:

                Assert.AreEqual failed. Expected:<None (Microsoft.CodeAnalysis.NoLocation)>. Actual:<SourceFile(/0/Test0.cs[103..115)) (Microsoft.CodeAnalysis.SourceLocation)>. Expected a project diagnostic with no location:

                Expected diagnostic:
                    // warning SA0007
                new DiagnosticResult(NamingAnalyzer.SA0007),

                Actual diagnostic:
                    // /0/Test0.cs(5,18): warning SA0007: '_class_name_' does not match the regular expression '^[A-Z][a-z]+(?:[A-Z][a-z]+)*$'
                VerifyCS.Diagnostic(NamingAnalyzer.SA0007).WithSpan(5, 18, 5, 30).WithArguments("_class_name_", "^[A-Z][a-z]+(?:[A-Z][a-z]+)*$"),
                */

                if (!ex.Message.Contains("Expected a project diagnostic with no location:"))
                    throw;

                IEnumerable<string> actualDiagnostics = ex.Message
                    .Split("Actual diagnostic:")[1]
                    .Split("\n", StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => s.Trim().StartsWith("//"))
                    .Select(s => s.Split(":")[1].Trim());

                foreach (DiagnosticResult diagnostic in expected)
                {
                    string diagnosticString = $"{diagnostic.Severity.ToString().ToLower()} {diagnostic.Id}";
                    Assert.IsTrue(actualDiagnostics.Contains(diagnosticString), $"Expected diagnostic '{diagnosticString}' not found.");
                }
            }
        }
    }
}
