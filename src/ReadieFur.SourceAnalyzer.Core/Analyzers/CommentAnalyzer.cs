using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Collections.Immutable;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CommentAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor DiagnosticDescriptor => new(
            id: Helpers.ANALYZER_ID_PREFIX + "0018",
            title: "Comment format.",
            messageFormat:
                "Comments should " + (ConfigManager.Configuration.Formatting.Comments.AddSpaceBeforeComment ? "" : "not")
                + " have a leading space.",
            category: "Formatting",
            defaultSeverity: ConfigManager.Configuration.Formatting.Comments.Severity.ToDiagnosticSeverity(),
            isEnabledByDefault: ConfigManager.Configuration.Formatting.Comments.IsEnabled);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxTreeAction(Analyze);
        }

        private void Analyze(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetRoot();

            foreach (SyntaxToken token in root.DescendantTokens(_ => true))
            {
                if (token.HasLeadingTrivia)
                    AnalyzeTrivia(context, token.LeadingTrivia);

                if (token.HasTrailingTrivia)
                    AnalyzeTrivia(context, token.TrailingTrivia);
            }
        }

        private void AnalyzeTrivia(SyntaxTreeAnalysisContext context, SyntaxTriviaList syntaxTrivias)
        {
            foreach (SyntaxTrivia trivia in syntaxTrivias)
            {
                switch (trivia.RawKind)
                {
                    case (int)SyntaxKind.SingleLineCommentTrivia:
                        //Single-line comments always have 2 leading characters to denote a comment in C# (either /* or //) so we can just check the third character.
                        //I will also check the fourth character as a saftey as commenting out code coult result in multiple spaces before the text whereas typically comments only have a single space before the text.
                        string text = trivia.ToString();
                        if (char.IsWhiteSpace(text[2]) && !char.IsWhiteSpace(text[3]))
                            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, trivia.GetLocation()));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
