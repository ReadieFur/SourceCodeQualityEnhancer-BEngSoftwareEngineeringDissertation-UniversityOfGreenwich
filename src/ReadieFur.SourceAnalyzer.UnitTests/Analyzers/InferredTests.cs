using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using ReadieFur.SourceAnalyzer.UnitTests.Verifiers;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class InferredTests : AAnalyzerTester
    {
        protected override FileInterpreter.CodeFixDiagnosticCallback CodeFixDiagnosticCallback => (diagnosticDescriptor, _) => CSharpCodeFixVerifier<InferredAnalyzer, InferredFixProvider>.Diagnostic(diagnosticDescriptor);

        private FileInterpreter.AnalyzerDiagnosticCallback AccessModifierAnalyzerCallback => (_, _) => InferredAnalyzer.AccessModifierDiagnosticDescriptor;
        [MTest] public async Task AccessModifierAnalyzer() => await TestAnalyzer<InferredAnalyzer>("Inferred_AccessModifier.cs", AccessModifierAnalyzerCallback);
        //This IS passing in it's current state, however the diff graph says it's incorrect, it is not (possibly a whitespace issue from what I can tell).
        //The issue seems to be related with the method declaration.
        [MTest] public async Task AccessModifierFixProvider() => await TestFixProvider<InferredAnalyzer, InferredFixProvider>("Inferred_AccessModifier.cs", AccessModifierAnalyzerCallback);

        private FileInterpreter.AnalyzerDiagnosticCallback NewKeywordAnalyzerCallback => (_, _) => InferredAnalyzer.NewKeywordDiagnosticDescriptor;
        [MTest] public async Task NewKeywordAnalyzer() => await TestAnalyzer<InferredAnalyzer>("Inferred_NewKeyword.cs", NewKeywordAnalyzerCallback);
        [MTest] public async Task NewKeywordFixProvider() => await TestFixProvider<InferredAnalyzer, InferredFixProvider>("Inferred_NewKeyword.cs", NewKeywordAnalyzerCallback);
    }
}
