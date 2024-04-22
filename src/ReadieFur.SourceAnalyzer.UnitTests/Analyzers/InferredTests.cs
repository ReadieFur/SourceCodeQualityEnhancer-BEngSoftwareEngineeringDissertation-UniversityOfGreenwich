using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using ReadieFur.SourceAnalyzer.UnitTests.Verifiers;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class InferredTests : AAnalyzerTester
    {
        protected override FileInterpreter.AnalyzerDiagnosticCallback AnalyzerDiagnosticCallback => (_, _) => InferredAnalyzer.AccessModifierDiagnosticDescriptor;
        protected override FileInterpreter.CodeFixDiagnosticCallback CodeFixDiagnosticCallback => (diagnosticDescriptor, _) => CSharpCodeFixVerifier<InferredAnalyzer, InferredFixProvider>.Diagnostic(diagnosticDescriptor);

        [MTest] public async Task InferredAccessModifierAnalyzer() => await TestAnalyzer<InferredAnalyzer>("Inferred_AccessModifier.cs");
        //This IS passing in it's current state, however the diff graph says it's incorrect, it is not (possibly a whitespace issue from what I can tell).
        //The issue seems to be related with the method declaration.
        [MTest] public async Task InferredAccessModifierFixProvider() => await TestFixProvider<InferredAnalyzer, InferredFixProvider>("Inferred_AccessModifier.cs");
    }
}
