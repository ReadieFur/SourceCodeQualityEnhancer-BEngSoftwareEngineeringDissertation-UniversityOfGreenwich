using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CommentAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor LeadingSpaceDiagnosticDescriptor => new(
            id: Helpers.ANALYZER_ID_PREFIX + "0018",
            title: "Comment format (prefix).",
            messageFormat:
                "Comments should " + (ConfigManager.Configuration.Formatting.Comments.LeadingSpace ? "" : "not")
                + " have a leading space.",
            category: "Formatting",
            defaultSeverity: ConfigManager.Configuration.Formatting.Comments.Severity.ToDiagnosticSeverity(),
            isEnabledByDefault: ConfigManager.Configuration.Formatting.Comments.IsEnabled);

        public static DiagnosticDescriptor TrailingFullStopDiagnosticDescriptor => new(
            id: Helpers.ANALYZER_ID_PREFIX + "0019",
            title: "Comment format (suffix).",
            messageFormat:
                "Comments should " + (ConfigManager.Configuration.Formatting.Comments.TrailingFullStop ? "" : "not")
                + " have a full stop at the end.",
            category: "Formatting",
            defaultSeverity: ConfigManager.Configuration.Formatting.Comments.Severity.ToDiagnosticSeverity(),
            isEnabledByDefault: ConfigManager.Configuration.Formatting.Comments.IsEnabled);

        public static DiagnosticDescriptor NewLineDiagnosticDescriptor => new(
            id: Helpers.ANALYZER_ID_PREFIX + "0020",
            title: "Comment format (line).",
            messageFormat:
                "Comments should " + (ConfigManager.Configuration.Formatting.Comments.NewLine ? "" : "not")
                + " be on their own line.",
            category: "Formatting",
            defaultSeverity: ConfigManager.Configuration.Formatting.Comments.Severity.ToDiagnosticSeverity(),
            isEnabledByDefault: ConfigManager.Configuration.Formatting.Comments.IsEnabled);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(LeadingSpaceDiagnosticDescriptor, TrailingFullStopDiagnosticDescriptor, NewLineDiagnosticDescriptor);

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

        private void AnalyzeTrivia(SyntaxTreeAnalysisContext context, SyntaxTriviaList syntaxTriviaList)
        {
            foreach (SyntaxTrivia trivia in syntaxTriviaList)
            {
                switch (trivia.RawKind)
                {
                    case (int)SyntaxKind.SingleLineCommentTrivia:
                        //Single-line comments always have 2 leading characters to denote a comment in C# (either /* or //) so we can just check the third character.
                        //I will also check the fourth character as a saftey as commenting out code coult result in multiple spaces before the text whereas typically comments only have a single space before the text.
                        string text = trivia.ToString();
                        if (char.IsWhiteSpace(text[2]) && !char.IsWhiteSpace(text[3]))
                            context.ReportDiagnostic(Diagnostic.Create(LeadingSpaceDiagnosticDescriptor, trivia.GetLocation()));

                        //FullStop.
                        //Check if the comment ends with an a-Z0-9 character, if it does then hint to add a full stop.
                        if (char.IsLetterOrDigit(text[text.TrimEnd().Length - 1]))
                            context.ReportDiagnostic(Diagnostic.Create(TrailingFullStopDiagnosticDescriptor, trivia.GetLocation()));

                        //NewLine.
                        Location triviaLocation = trivia.GetLocation();
                        FileLinePositionSpan triviaLineSpan = triviaLocation.GetLineSpan();
                        FileLinePositionSpan tokenLineSpan = trivia.Token.GetLocation().GetLineSpan();
                        if (triviaLineSpan.StartLinePosition.Line >= tokenLineSpan.StartLinePosition.Line && triviaLineSpan.EndLinePosition.Line <= tokenLineSpan.EndLinePosition.Line)
                            context.ReportDiagnostic(Diagnostic.Create(NewLineDiagnosticDescriptor, triviaLocation));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
