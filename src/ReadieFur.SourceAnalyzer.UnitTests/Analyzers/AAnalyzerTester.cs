using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using ReadieFur.SourceAnalyzer.Core.Configuration;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    public abstract class AAnalyzerTester
    {
        protected virtual FileInterpreter.AnalyzerDiagnosticCallback AnalyzerDiagnosticCallback => throw new NotImplementedException();

        protected virtual FileInterpreter.CodeFixDiagnosticCallback CodeFixDiagnosticCallback => (_, _) => new DiagnosticResult();

        private async Task<FileInterpreter> Init(
            string fileName,
            FileInterpreter.AnalyzerDiagnosticCallback? analyzerDiagnosticCallback = null,
            FileInterpreter.CodeFixDiagnosticCallback? codeFixDiagnosticCallback = null,
            ConfigRoot? configuration = null)
        {
            if (configuration is not null)
                Helpers.SetConfiguration(configuration);
            else
                await Helpers.LoadConfiguration();
            return await FileInterpreter.Interpret(fileName, analyzerDiagnosticCallback ?? AnalyzerDiagnosticCallback, codeFixDiagnosticCallback ?? CodeFixDiagnosticCallback);
        }

        protected async Task TestAnalyzer<TAnalyzer>(
            string fileName, FileInterpreter.AnalyzerDiagnosticCallback? analyzerDiagnosticCallback = null, ConfigRoot? configuration = null)
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            FileInterpreter file = await Init(fileName, analyzerDiagnosticCallback, configuration: configuration);
            await Verifiers.CSharpAnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(file.AnalyzerInput, file.AnalyzerDiagnostics);
        }

        protected async Task TestFixProvider<TAnalyzer, TFixProvider>(
            string fileName, FileInterpreter.AnalyzerDiagnosticCallback? analyzerDiagnosticCallback = null, FileInterpreter.CodeFixDiagnosticCallback? codeFixDiagnosticCallback = null, ConfigRoot? configuration = null)
            where TAnalyzer : DiagnosticAnalyzer, new() where TFixProvider : CodeFixProvider, new()
        {
            FileInterpreter file = await Init(fileName, analyzerDiagnosticCallback, codeFixDiagnosticCallback, configuration);
            await Verifiers.CSharpCodeFixVerifier<TAnalyzer, TFixProvider>.VerifyCodeFixAsync(file.CodeFixInput, file.CodeFixDiagnostics, file.CodeFixExpected);
        }
    }
}
