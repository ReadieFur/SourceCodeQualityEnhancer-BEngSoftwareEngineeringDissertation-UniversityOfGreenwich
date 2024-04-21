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
            title: "Operand space",
            messageFormat: "The operand '{0}' should" + (ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround?.Required is true ? "" : " not") + " have spaces either side of it.",
            category: "Formatting",
            defaultSeverity: Helpers.GetDiagnosticSeverity(ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround?.Severity),
            isEnabledByDefault: ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround is not null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SpaceDiagnosticDescriptor);

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
            }
        }

        private void AnalyzeSpace(SyntaxTreeAnalysisContext context, SyntaxToken token)
        {
            if (ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround is null || !ConfigManager.Configuration.Formatting.Punctuation.SpaceAround.Tokens.Contains((SyntaxKind)token.RawKind))
                return;

            //TODO: Check for space around the token.
            //To reliably do this we must check; the token's leading and trailing trivia, the previous token's trailing trivia, and the next token's leading trivia.
            //This is because the token's leading and trailing trivia may not be the spaces we are looking for, they may be comments or other whitespace.
            //For this I will include any whitespace trivia as a space, i.e. EOL, tab, space, etc.
            //Additionally if a space is to be required then we should check to see if multiple spaces exist and if so report that as an error.

            SyntaxKind[] whitespaceTrivia = [SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia];

            int leadingWhitespace = token.LeadingTrivia.Concat(token.GetPreviousToken().TrailingTrivia).Count(t => whitespaceTrivia.Contains(t.Kind()));
            int trailingTrivia = token.TrailingTrivia.Concat(token.GetNextToken().LeadingTrivia).Count(t => whitespaceTrivia.Contains(t.Kind()));

            if ((ConfigManager.Configuration.Formatting.Punctuation.SpaceAround.Required && (leadingWhitespace != 1 || trailingTrivia != 1))
                || (!ConfigManager.Configuration.Formatting.Punctuation.SpaceAround.Required && (leadingWhitespace != 0 || trailingTrivia != 0)))
                context.ReportDiagnostic(Diagnostic.Create(SpaceDiagnosticDescriptor, token.GetLocation(), token.ValueText));
        }
    }
}
