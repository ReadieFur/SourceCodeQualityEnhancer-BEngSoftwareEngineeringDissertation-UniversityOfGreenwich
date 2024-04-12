using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    public abstract class AAnalyzerTester
    {
        protected abstract FileInterpreter.AnalyzerDiagnosticCallback AnalyzerDiagnosticCallback { get; }

        protected abstract FileInterpreter.CodeFixDiagnosticCallback CodeFixDiagnosticCallback { get; }

        private async Task<FileInterpreter> Init(string fileName)
        {
            await Helpers.LoadConfiguration();
            return await FileInterpreter.Interpret(fileName, AnalyzerDiagnosticCallback, CodeFixDiagnosticCallback);
        }

        protected async Task TestAnalyzer<TAnalyzer>(string fileName) where TAnalyzer : DiagnosticAnalyzer, new()
        {
            FileInterpreter file = await Init(fileName);
            await Verifiers.CSharpAnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(file.AnalyzerInput, file.AnalyzerDiagnostics);
        }

        protected async Task TestFixProvider<TAnalyzer, TFixProvider>(string fileName) where TAnalyzer : DiagnosticAnalyzer, new() where TFixProvider : CodeFixProvider, new()
        {
            FileInterpreter file = await Init(fileName);
            await Verifiers.CSharpCodeFixVerifier<TAnalyzer, TFixProvider>.VerifyCodeFixAsync(file.CodeFixInput, file.CodeFixDiagnostics, file.CodeFixExpected);
        }
    }
}
