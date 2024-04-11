using Microsoft;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    public abstract class AAnalyzerTester//<TAnalyzer, TFixProvider> where TAnalyzer : DiagnosticAnalyzer, new() where TFixProvider : CodeFixProvider, new()
    {
        protected async Task TestAnalyzer<TAnalyzer>(string fileName) where TAnalyzer : DiagnosticAnalyzer, new()
        {
            await LoadConfiguration();
            FileInterpreter file = await FileInterpreter.Interpret(fileName);
            await Verifiers.CSharpAnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(file.AnalyzerInput, file.AnalyzerDiagnostics);
        }

        protected async Task TestFixProvider<TAnalyzer, TFixProvider>(string fileName) where TAnalyzer : DiagnosticAnalyzer, new() where TFixProvider : CodeFixProvider, new()
        {
            await LoadConfiguration();
            FileInterpreter file = await FileInterpreter.Interpret(fileName);
            await Verifiers.CSharpCodeFixVerifier<TAnalyzer, TFixProvider>.VerifyCodeFixAsync(file.CodeFixInput, file.CodeFixDiagnostics, file.CodeFixExpected);
        }
    }
}
