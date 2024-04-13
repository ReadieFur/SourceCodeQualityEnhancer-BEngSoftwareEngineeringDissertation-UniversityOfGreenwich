using Microsoft.CodeAnalysis;
using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using System.Collections.Concurrent;
using Fixer = ReadieFur.SourceAnalyzer.UnitTests.Verifiers.CSharpCodeFixVerifier<ReadieFur.SourceAnalyzer.Core.Analyzers.BraceLocationAnalyzer, ReadieFur.SourceAnalyzer.Core.Analyzers.BraceLocationFixProvider>;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class FormattingTests : AAnalyzerTester
    {
        private ConcurrentDictionary<DiagnosticDescriptor, string> instances = new();

        protected override FileInterpreter.AnalyzerDiagnosticCallback AnalyzerDiagnosticCallback =>
            (_, _) => Core.Analyzers.BraceLocationAnalyzer.DiagnosticDescriptor;

        protected override FileInterpreter.CodeFixDiagnosticCallback CodeFixDiagnosticCallback =>
            (diagnosticDescriptor, _) => Fixer.Diagnostic(diagnosticDescriptor);

        [MTest] public async Task BraceLocationAnalyzer() => await TestAnalyzer<BraceLocationAnalyzer>("CurlyBrace.cs");

        [MTest] public async Task BraceLocationFixProvider() => await TestFixProvider<BraceLocationAnalyzer, BraceLocationFixProvider>("CurlyBrace.cs");
    }
}
