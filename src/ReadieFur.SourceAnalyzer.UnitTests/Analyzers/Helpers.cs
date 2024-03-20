using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    public abstract class AAnalyzerTester//<TAnalyzer, TFixProvider> where TAnalyzer : DiagnosticAnalyzer, new() where TFixProvider : CodeFixProvider, new()
    {
        protected async Task<FileInterpreter> CommonTasks(Type sourceType)
        {
            await LoadConfiguration();
            return await FileInterpreter.Interpret(sourceType);
        }

        protected async Task TestAnalyzer<TAnalyzer>(Type sourceType) where TAnalyzer : DiagnosticAnalyzer, new()
        {
            FileInterpreter file = await CommonTasks(sourceType);
            await Verifiers.CSharpAnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(file.AnalyzerInput, file.AnalyzerDiagnostics);
        }

        protected async Task TestFixProvider<TAnalyzer, TFixProvider>(Type sourceType) where TAnalyzer : DiagnosticAnalyzer, new() where TFixProvider : CodeFixProvider, new()
        {
            FileInterpreter file = await CommonTasks(sourceType);
            await Verifiers.CSharpCodeFixVerifier<TAnalyzer, TFixProvider>.VerifyCodeFixAsync(file.CodeFixInput, file.CodeFixDiagnostics, file.CodeFixExpected);
        }
    }
}
