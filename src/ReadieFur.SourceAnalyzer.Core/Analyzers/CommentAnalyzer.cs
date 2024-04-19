using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

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
                        string rawText = trivia.ToString();

                        #region Comment detection
                        //Typically comments contain individual words seperated by a single space, we can check the text to see if it adhears to something that looks like an english string or a piece of code.
                        //If the text is less than 4 characters, we canno't work on it.
                        if (rawText.Length < 4)
                            break;

                        //Find the index of the first character, skipping the comment delimiter.
                        int charStart = rawText.IndexOf(rawText.Skip(2).First(c => !char.IsWhiteSpace(c)));
                        //If the first non-whitespace character is after index 3 then the comment is likely commented out code and so we should ignore it.
                        if (charStart > 3)
                            break;
                        string text = rawText.Substring(charStart);

                        //We can use regex to check for words that are either all lowercase or start with a capital and then only lowercase letters after that.
                        //If the char count of regular words is greater than x% of the string, then assume it is a comment and not code.
                        int matchLength = 0;
                        /* Regex breakdown:
                         * (?<![.]) Don't capture when the text follows a full-stop.
                         * \b limit the search to be between word boundaries (i.e. is a letter followed by a non-letter)
                         * (?: capture everything inside as a single match
                         * [A-Z][a-z]* capture a Capital and optinally unlimited lowercase letters, or...
                         * ... [a-z]+ capture one or more lowercase letters, or...
                         * ... Include spaces in the search captures.
                         * (?![.(][A-z]) Don't capture when the sequence ends with either a . or ( as these are common code tokens that often directly proceed text, so long as it is followed by another A-z character.
                         */
                        foreach (Match match in Regex.Matches(text, @"(?<![.])\b(?:[A-Z][a-z]*|[a-z]+| )\b(?![.(][A-z])"))
                            matchLength += match.Length;

                        //The search threshold dosen't have to be that high as some comments may reference code, so I will set the threshold to be at 50% for now.
                        //TODO: Make this parameter configurable.
                        if ((double)matchLength / text.Length < 0.5d)
                            break;
                        #endregion

                        //Space.
                        if ((ConfigManager.Configuration.Formatting.Comments.LeadingSpace && !char.IsWhiteSpace(rawText[2])) ||
                            (!ConfigManager.Configuration.Formatting.Comments.LeadingSpace && char.IsWhiteSpace(rawText[2])))
                            context.ReportDiagnostic(Diagnostic.Create(LeadingSpaceDiagnosticDescriptor, trivia.GetLocation()));

                        //FullStop.
                        //Check if the comment ends with an a-Z0-9 character, if it does then hint to add a full stop.
                        if (char.IsLetterOrDigit(text[text.TrimEnd().Length - 1]))
                            context.ReportDiagnostic(Diagnostic.Create(TrailingFullStopDiagnosticDescriptor, trivia.GetLocation()));

                        //NewLine.
                        Location triviaLocation = trivia.GetLocation();
                        FileLinePositionSpan triviaLineSpan = triviaLocation.GetLineSpan();
                        FileLinePositionSpan tokenLineSpan = trivia.Token.GetLocation().GetLineSpan();
                        if ((ConfigManager.Configuration.Formatting.Comments.NewLine && triviaLineSpan.StartLinePosition.Line >= tokenLineSpan.StartLinePosition.Line && triviaLineSpan.EndLinePosition.Line <= tokenLineSpan.EndLinePosition.Line)
                            || (!ConfigManager.Configuration.Formatting.Comments.NewLine && triviaLineSpan.StartLinePosition.Line != tokenLineSpan.StartLinePosition.Line))
                            context.ReportDiagnostic(Diagnostic.Create(NewLineDiagnosticDescriptor, triviaLocation));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
