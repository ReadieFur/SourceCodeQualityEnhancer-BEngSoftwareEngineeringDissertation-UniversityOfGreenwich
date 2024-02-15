using Microsoft.CodeAnalysis.Testing;
using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using ReadieFur.SourceAnalyzer.UnitTests.TestFiles;
using Analyzer = ReadieFur.SourceAnalyzer.UnitTests.Verifiers.CSharpAnalyzerVerifier<ReadieFur.SourceAnalyzer.Core.Analyzers.NamingAnalyzer>;
using CodeFixer = ReadieFur.SourceAnalyzer.UnitTests.Verifiers.CSharpCodeFixVerifier<ReadieFur.SourceAnalyzer.Core.Analyzers.NamingAnalyzer, ReadieFur.SourceAnalyzer.Core.Analyzers.NamingFixProvider>;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class NamingTests
    {
        private static async Task TestAnalyzer(Type sourceType, params ENamingAnalyzer[] namingAnalyzer)
        {
            await LoadConfiguration();

            //Get the source file for the provided type.
            if (await GetSourceFile(sourceType) is not string sourceFileContents)
            {
                Assert.Fail("Could not find source file.");
                return;
            }

            //Verify the configuration.
            List<DiagnosticResult> diagnostics = new();
            foreach (ENamingAnalyzer analyzer in namingAnalyzer)
            {
                if (typeof(Naming).GetProperty(analyzer.ToString())!.GetValue(ConfigManager.Configuration.Naming) is not NamingConvention namingConvention)
                {
                    Assert.Fail($"Could not find naming convention for '{analyzer}'.");
                    return;
                }

                if (!Core.Analyzers.Helpers.TryGetAnalyzerID<ENamingAnalyzer>(analyzer.ToString(), out string analyzerID, out _))
                {
                    Assert.Fail($"Could not find analyzer ID for '{analyzer}'.");
                    return;
                }

                diagnostics.Add(new(analyzerID, namingConvention.Severity.ToDiagnosticSeverity()));
            }

            //Verify the analyzers.
            await Analyzer.VerifyAnalyzerAsync(sourceFileContents, diagnostics.ToArray());
        }

        [MTest]
        public async Task TestClassName() => await TestAnalyzer(typeof(_class_name_), ENamingAnalyzer.Class);
    }
}
