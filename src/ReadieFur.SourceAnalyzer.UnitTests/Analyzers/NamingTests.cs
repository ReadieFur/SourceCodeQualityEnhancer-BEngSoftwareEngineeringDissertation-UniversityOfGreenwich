using Microsoft.CodeAnalysis.Testing;
using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using ReadieFur.SourceAnalyzer.UnitTests.TestFiles;
using System.Collections.Immutable;
using Analyzer = ReadieFur.SourceAnalyzer.UnitTests.Verifiers.CSharpAnalyzerVerifier<ReadieFur.SourceAnalyzer.Core.Analyzers.NamingAnalyzer>;
using CodeFixer = ReadieFur.SourceAnalyzer.UnitTests.Verifiers.CSharpCodeFixVerifier<ReadieFur.SourceAnalyzer.Core.Analyzers.NamingAnalyzer, ReadieFur.SourceAnalyzer.Core.Analyzers.NamingFixProvider>;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class NamingTests
    {
        private static async Task<FileInterpreter> CommonTasks(Type sourceType)
        {
            await LoadConfiguration();
            return await FileInterpreter.Interpret(sourceType);
        }

        private static async Task TestAnalyzer(Type sourceType)
        {
            FileInterpreter file = await CommonTasks(sourceType);
            await Analyzer.VerifyAnalyzerAsync(file.AnalyzerInput, file.AnalyzerDiagnostics);
        }

        private static async Task TestFixProvider(Type sourceType)
        {
            FileInterpreter file = await CommonTasks(sourceType);
            
            await CodeFixer.VerifyCodeFixAsync(
                file.CodeFixInput,
                //TODO: Figure out why when providing the expected diagnostics results the test fails due to it believing one more diagnostic has been provided than what has actually been provided.
                //UPDATE: It could be failing becuase my code fix provider has not been properly implemented yet.
#if true
                file.AnalyzerDiagnostics,
#else
                new DiagnosticResult[0],
#endif
                file.CodeFixExpected);
        }

        [MTest]
        public async Task TestClassNameAnalyzer() => await TestAnalyzer(typeof(_class_name_));

        [MTest]
        public async Task TestClassNameFixProvider() => await TestFixProvider(typeof(_class_name_));
    }
}
