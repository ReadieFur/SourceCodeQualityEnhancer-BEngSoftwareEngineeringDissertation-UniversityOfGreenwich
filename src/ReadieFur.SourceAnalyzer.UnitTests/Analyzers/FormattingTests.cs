using Microsoft.CodeAnalysis;
using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using ReadieFur.SourceAnalyzer.UnitTests.Verifiers;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class FormattingTests : AAnalyzerTester
    {
        private FileInterpreter.AnalyzerDiagnosticCallback BraceAnalyzerDiagnosticCallback => (_, _) => Core.Analyzers.BraceLocationAnalyzer.DiagnosticDescriptor;
        private FileInterpreter.CodeFixDiagnosticCallback BraceCodeFixDiagnosticCallback => (diagnosticDescriptor, _) => CSharpCodeFixVerifier<BraceLocationAnalyzer, BraceLocationFixProvider>.Diagnostic(diagnosticDescriptor);
        [MTest] public async Task BraceLocationAnalyzer() => await TestAnalyzer<BraceLocationAnalyzer>("CurlyBrace.cs", BraceAnalyzerDiagnosticCallback);
        [MTest] public async Task BraceLocationFixProvider() => await TestFixProvider<BraceLocationAnalyzer, BraceLocationFixProvider>("CurlyBrace.cs", BraceAnalyzerDiagnosticCallback, BraceCodeFixDiagnosticCallback);

        private FileInterpreter.AnalyzerDiagnosticCallback IndentationAnalyzerDiagnosticCallback => (_, input) => new(
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.Id,
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.Title,
            string.Format(Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.MessageFormat.ToString(), input),
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.Category,
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.DefaultSeverity,
            Core.Analyzers.IndentationAnalyzer.DiagnosticDescriptor.IsEnabledByDefault
        );
        private FileInterpreter.CodeFixDiagnosticCallback IndentationCodeFixDiagnosticCallback => (diagnosticDescriptor, _) => CSharpCodeFixVerifier<IndentationAnalyzer, BraceLocationFixProvider>.Diagnostic(diagnosticDescriptor);
        [MTest] public async Task IndentationAnalyzer() => await TestAnalyzer<IndentationAnalyzer>("Indentation.cs", IndentationAnalyzerDiagnosticCallback);
        [MTest] public async Task IndentationFixProvider() => await TestFixProvider<IndentationAnalyzer, IndentationFixProvider>("Indentation.cs", IndentationAnalyzerDiagnosticCallback, IndentationCodeFixDiagnosticCallback);
    }
}
