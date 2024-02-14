using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ReadieFur.SourceAnalyzer.VSIX
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DudAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create<DiagnosticDescriptor>();

        public override void Initialize(AnalysisContext context) { }
    }
}
