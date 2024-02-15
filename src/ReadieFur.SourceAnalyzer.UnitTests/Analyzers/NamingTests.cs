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
        private static async Task<SFileInterpreter> CommonTasks(Type sourceType)
        {
            await LoadConfiguration();
            return await SFileInterpreter.Interpret(sourceType);
        }

        private static async Task TestAnalyzer(Type sourceType, params ENamingAnalyzer[] namingAnalyzers)
        {
            SFileInterpreter source = await CommonTasks(sourceType);

            //Verify the configuration.
            List<DiagnosticResult> diagnostics = new();
            foreach (ENamingAnalyzer namingAnalyzer in namingAnalyzers)
            {
                if (typeof(Naming).GetProperty(namingAnalyzer.ToString())!.GetValue(ConfigManager.Configuration.Naming) is not NamingConvention namingConvention)
                {
                    Assert.Fail($"Could not find naming convention for '{namingAnalyzer}'.");
                    return;
                }

                if (!Core.Analyzers.Helpers.TryGetAnalyzerID<ENamingAnalyzer>(namingAnalyzer.ToString(), out string analyzerID, out _))
                {
                    Assert.Fail($"Could not find analyzer ID for '{namingAnalyzer}'.");
                    return;
                }

                //TODO: Use the diagnostics with positional arguments.
                diagnostics.Add(new(analyzerID, namingConvention.Severity.ToDiagnosticSeverity()));
            }

            //Verify the analyzers.
            await Analyzer.VerifyAnalyzerAsync(source.interpretedText, diagnostics.ToArray());
        }

        private static async Task TestFixProvider(Type sourceType, params ENamingAnalyzer[] namingAnalyzers)
        {
            SFileInterpreter source = await CommonTasks(sourceType);
        }

        [MTest]
        public async Task TestClassNameAnalyzer() => await TestAnalyzer(typeof(_class_name_), ENamingAnalyzer.Class);

        [MTest]
        public async Task TestClassNameFixProvider() => await TestFixProvider(typeof(_class_name_), ENamingAnalyzer.Class);
    }
}
