using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using ReadieFur.SourceAnalyzer.UnitTests.Verifiers;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class FormattingTests : AAnalyzerTester
    {
        private FileInterpreter.AnalyzerDiagnosticCallback BraceAnalyzerDiagnosticCallback => (_, _) => Core.Analyzers.BraceAnalyzer.DiagnosticDescriptor;
        private FileInterpreter.CodeFixDiagnosticCallback BraceCodeFixDiagnosticCallback => (diagnosticDescriptor, _) => CSharpCodeFixVerifier<BraceAnalyzer, BraceFixProvider>.Diagnostic(diagnosticDescriptor);
        [MTest] public async Task BraceLocationAnalyzer() => await TestAnalyzer<BraceAnalyzer>("CurlyBrace.cs", BraceAnalyzerDiagnosticCallback);
        [MTest] public async Task BraceLocationFixProvider() => await TestFixProvider<BraceAnalyzer, BraceFixProvider>("CurlyBrace.cs", BraceAnalyzerDiagnosticCallback, BraceCodeFixDiagnosticCallback);

        private FileInterpreter.AnalyzerDiagnosticCallback IndentationAnalyzerDiagnosticCallback => (_, input) => new(
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.Id,
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.Title,
            string.Format(Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.MessageFormat.ToString(), input),
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.Category,
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.DefaultSeverity,
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.IsEnabledByDefault
        );
        private FileInterpreter.CodeFixDiagnosticCallback IndentationCodeFixDiagnosticCallback => (diagnosticDescriptor, _) => CSharpCodeFixVerifier<IndentationAnalyzer, BraceFixProvider>.Diagnostic(diagnosticDescriptor);
        [MTest] public async Task IndentationAnalyzer() => await TestAnalyzer<IndentationAnalyzer>("Indentation.cs", IndentationAnalyzerDiagnosticCallback);
        [MTest] public async Task IndentationFixProvider() => await TestFixProvider<IndentationAnalyzer, IndentationFixProvider>("Indentation.cs", IndentationAnalyzerDiagnosticCallback, IndentationCodeFixDiagnosticCallback);
    }
}
