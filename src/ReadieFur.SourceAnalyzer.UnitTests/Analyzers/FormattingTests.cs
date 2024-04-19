using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using ReadieFur.SourceAnalyzer.UnitTests.Verifiers;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class FormattingTests : AAnalyzerTester
    {
        private FileInterpreter.AnalyzerDiagnosticCallback BraceAnalyzerDiagnosticCallback => (_, _) => BraceAnalyzer.DiagnosticDescriptor;
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

        private FileInterpreter.AnalyzerDiagnosticCallback CommentLeadingSpaceAnalyzerDiagnosticCallback => (_, _) => CommentAnalyzer.LeadingSpaceDiagnosticDescriptor;
        private FileInterpreter.CodeFixDiagnosticCallback CommentCodeFixDiagnosticCallback => (diagnosticDescriptor, _) => CSharpCodeFixVerifier<CommentAnalyzer, CommentFixProvider>.Diagnostic(diagnosticDescriptor);
        [MTest] public async Task CommentLeadingSpaceAnalyzer() => await TestAnalyzer<CommentAnalyzer>("Comment_LeadingSpace.cs", CommentLeadingSpaceAnalyzerDiagnosticCallback);
        [MTest] public async Task CommentLeadingSpaceProvider() => await TestFixProvider<CommentAnalyzer, CommentFixProvider>("Comment_LeadingSpace.cs", CommentLeadingSpaceAnalyzerDiagnosticCallback, CommentCodeFixDiagnosticCallback);

        private FileInterpreter.AnalyzerDiagnosticCallback CommentTrailingFullStopAnalyzerDiagnosticCallback => (_, _) => CommentAnalyzer.TrailingFullStopDiagnosticDescriptor;
        [MTest] public async Task CommentTrailingFullStopAnalyzer() => await TestAnalyzer<CommentAnalyzer>("Comment_TrailingFullStop.cs", CommentTrailingFullStopAnalyzerDiagnosticCallback);
        [MTest] public async Task CommentTrailingFullStopFixProvider() => await TestFixProvider<CommentAnalyzer, CommentFixProvider>("Comment_TrailingFullStop.cs", CommentTrailingFullStopAnalyzerDiagnosticCallback, CommentCodeFixDiagnosticCallback);

        private FileInterpreter.AnalyzerDiagnosticCallback CommentNewLineAnalyzerDiagnosticCallback => (_, _) => CommentAnalyzer.NewLineDiagnosticDescriptor;
        [MTest] public async Task CommentNewLineAnalyzer() => await TestAnalyzer<CommentAnalyzer>("Comment_NewLine.cs", CommentNewLineAnalyzerDiagnosticCallback);
        [MTest] public async Task CommentNewLineFixProvider() => await TestFixProvider<CommentAnalyzer, CommentFixProvider>("Comment_NewLine.cs", CommentNewLineAnalyzerDiagnosticCallback, CommentCodeFixDiagnosticCallback);
        [MTest] public async Task CommentOldLineFixProvider() => await TestFixProvider<CommentAnalyzer, CommentFixProvider>(
            "Comment_OldLine.cs", CommentNewLineAnalyzerDiagnosticCallback, CommentCodeFixDiagnosticCallback, new Core.Configuration.ConfigRoot
            {
                Formatting = new Core.Configuration.Formatting
                {
                    Comments = new Core.Configuration.Comments
                    {
                        NewLine = false
                    }
                }
            });
    }
}
