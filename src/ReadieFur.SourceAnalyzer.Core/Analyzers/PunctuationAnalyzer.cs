using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class PunctuationAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor SpaceDiagnosticDescriptor => new(
            id: EAnalyzerID.Punctuation_Space.ToTag(),
            title: "Punctuation space",
            //messageFormat: "The operand '{0}' should{1}.",
            messageFormat: "The punctuation '{0}' does not have the correct spacing around it.",
            category: "Formatting",
            defaultSeverity: DiagnosticSeverity.Info, //Fallback value.
            isEnabledByDefault: ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround is not null);

        public static DiagnosticDescriptor NewLineDiagnosticDescriptor => new(
            id: EAnalyzerID.Punctuation_NewLine.ToTag(),
            title: "Punctuation new line",
            //messageFormat: "The operand '{0}' should{1}.",
            messageFormat: "The punctuation '{0}' does not have the correct line spacing around it.",
            category: "Formatting",
            defaultSeverity: DiagnosticSeverity.Info, //Fallback value.
            isEnabledByDefault: ConfigManager.Configuration.Formatting?.Punctuation?.NewLine is not null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SpaceDiagnosticDescriptor, NewLineDiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //The below won't work because we need to analyze tokens and not nodes, unfortunately there is only a register node action available so we must instead analyze the entire tree instead.
            //context.RegisterSyntaxNodeAction(AnalyzeSpace, PunctuationToken.Punctuation.Values.ToArray());

            context.RegisterSyntaxTreeAction(Analyze);
        }

        private void Analyze(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetRoot();
            foreach (SyntaxToken token in root.DescendantTokens(_ => true))
            {
                AnalyzeSpace(context, token);
                AnalyzeNewLine(context, token);
            }
        }

        private void AnalyzeSpace(SyntaxTreeAnalysisContext context, SyntaxToken token)
        {
            if (ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround is null)
                return;

            //TODO: Check for space around the token.
            //To reliably do this we must check; the token's leading and trailing trivia, the previous token's trailing trivia, and the next token's leading trivia.
            //This is because the token's leading and trailing trivia may not be the spaces we are looking for, they may be comments or other whitespace.
            //For this I will include any whitespace trivia as a space, i.e. EOL, tab, space, etc.
            //Additionally if a space is to be required then we should check to see if multiple spaces exist and if so report that as an error.

            //Attempt to find the first configuration that matches the token.
            AroundToken? config = ConfigManager.Configuration.Formatting.Punctuation.SpaceAround.FirstOrDefault(c => c.Tokens.Contains(token.Kind()));
            if (config is null)
                return;

            //Check if there is any leading/trailing whitespace, delimited by EOL.
            IEnumerable<SyntaxTrivia> leadingTrivia = token.GetPreviousToken().TrailingTrivia.Concat(token.LeadingTrivia);
            IEnumerable<SyntaxTrivia> trailingTrivia = token.TrailingTrivia.Concat(token.GetNextToken().LeadingTrivia);
            int eolIndex;
            if ((eolIndex = leadingTrivia.ToList().FindLastIndex(t => t.IsKind(SyntaxKind.EndOfLineTrivia))) != -1)
                leadingTrivia = leadingTrivia.Skip(eolIndex + 1);
            if ((eolIndex = trailingTrivia.ToList().FindIndex(t => t.IsKind(SyntaxKind.EndOfLineTrivia))) != -1)
                trailingTrivia = trailingTrivia.Take(eolIndex + 1);
            SyntaxKind[] whitespaceTrivia = [SyntaxKind.WhitespaceTrivia/*, SyntaxKind.EndOfLineTrivia*/];
            bool hasLeadingWhitespace = leadingTrivia.Any(t => whitespaceTrivia.Contains(t.Kind()));
            bool hasTrailingTrivia = trailingTrivia.Any(t => whitespaceTrivia.Contains(t.Kind()));

            //StringBuilder sb = new();

            int left;
            if (config.Left && !hasLeadingWhitespace)
            {
                left = 1;
                //sb.Append(" have a space on the left ");
            }
            else if (!config.Left && hasLeadingWhitespace)
            {
                left = -1;
                //sb.Append(" not have a space on the left ");
            }
            else
            {
                left = 0;
            }

            int right;
            if (config.Right && !hasTrailingTrivia)
            {
                right = 1;
                /*if (sb.Length > 0)
                    sb.Append("and ");
                sb.Append("have a space on the right");*/
            }
            else if (!config.Right && hasTrailingTrivia)
            {
                right = -1;
                /*if (sb.Length > 0)
                    sb.Append("and ");
                sb.Append("not have a space on the right");*/
            }
            else
            {
                right = 0;
            }

            if (left == 0 && right == 0)
                return;

            ImmutableDictionary<string, string> diagnosticProps = new Dictionary<string, string>()
            {
                { "left", left.ToString() },
                { "right", right.ToString() }
            }.ToImmutableDictionary();

            DiagnosticDescriptor diagnosticDescriptor = new(
                id: SpaceDiagnosticDescriptor.Id,
                title: SpaceDiagnosticDescriptor.Title,
                messageFormat: SpaceDiagnosticDescriptor.MessageFormat,
                category: SpaceDiagnosticDescriptor.Category,
                defaultSeverity: config.Severity.ToDiagnosticSeverity(),
                isEnabledByDefault: SpaceDiagnosticDescriptor.IsEnabledByDefault
            );

            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, token.GetLocation(), properties: diagnosticProps, token.Text/*, sb.ToString()*/));
        }

        private void AnalyzeNewLine(SyntaxTreeAnalysisContext context, SyntaxToken token)
        {
            if (ConfigManager.Configuration.Formatting?.Punctuation?.NewLine is null)
                return;

            AroundToken? config = ConfigManager.Configuration.Formatting.Punctuation.NewLine.FirstOrDefault(c => c.Tokens.Contains(token.Kind()));
            if (config is null)
                return;

            IEnumerable<SyntaxTrivia> leadingTrivia = token.GetPreviousToken().TrailingTrivia.Concat(token.LeadingTrivia);
            IEnumerable<SyntaxTrivia> trailingTrivia = token.TrailingTrivia.Concat(token.GetNextToken().LeadingTrivia);
            bool hasLeadingNewLine = leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            bool hasTrailingNewLine = trailingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

            int left;
            if (config.Left && !hasLeadingNewLine)
            {
                left = 1;
            }
            else if (!config.Left && hasLeadingNewLine)
            {
                left = -1;
            }
            else
            {
                left = 0;
            }

            int right;
            if (config.Right && !hasTrailingNewLine)
            {
                right = 1;
            }
            else if (!config.Right && hasTrailingNewLine)
            {
                right = -1;
            }
            else
            {
                right = 0;
            }

            if (left == 0 && right == 0)
                return;

            ImmutableDictionary<string, string> diagnosticProps = new Dictionary<string, string>()
            {
                { "left", left.ToString() },
                { "right", right.ToString() }
            }.ToImmutableDictionary();

            DiagnosticDescriptor diagnosticDescriptor = new(
                id: NewLineDiagnosticDescriptor.Id,
                title: NewLineDiagnosticDescriptor.Title,
                messageFormat: NewLineDiagnosticDescriptor.MessageFormat,
                category: NewLineDiagnosticDescriptor.Category,
                defaultSeverity: config.Severity.ToDiagnosticSeverity(),
                isEnabledByDefault: NewLineDiagnosticDescriptor.IsEnabledByDefault
            );

            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, token.GetLocation(), properties: diagnosticProps, token.Text));
        }
    }
}
