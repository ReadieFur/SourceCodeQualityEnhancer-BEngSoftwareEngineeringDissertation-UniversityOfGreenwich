using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class FormattingTests : AAnalyzerTester
    {
        protected override FileInterpreter.AnalyzerDiagnosticCallback AnalyzerDiagnosticCallback => throw new NotImplementedException();

        protected override FileInterpreter.CodeFixDiagnosticCallback CodeFixDiagnosticCallback => throw new NotImplementedException();

        [MTest] public async Task BraceLocationAnalyzer() => await TestAnalyzer<BraceLocationAnalyzer>("CurlyBrace.cs");

        [MTest] public async Task BraceLocationFixProvider() => await TestFixProvider<BraceLocationAnalyzer, BraceLocationFixProvider>("CurlyBrace.cs");
    }
}
