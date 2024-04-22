using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class PunctuationFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PunctuationAnalyzer.SpaceDiagnosticDescriptor.Id);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == PunctuationAnalyzer.SpaceDiagnosticDescriptor.Id)
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: "Correct spacing",
                        createChangedDocument: ct => CorrectSpacingAsync(context.Document, diagnostic.Location.SourceSpan, diagnostic.Properties, ct),
                        equivalenceKey: "Punctuation"),
                        diagnostic);
                }   
            }

            return Task.CompletedTask;
        }

        private async Task<Document> CorrectSpacingAsync(Document document, TextSpan sourceSpan, ImmutableDictionary<string, string> properties, CancellationToken ct)
        {
            if (!properties.TryGetValue("left", out string _left) || !properties.TryGetValue("right", out string _right)
                || !int.TryParse(_left, out int left) || !int.TryParse(_right, out int right))
                return document;

            if (await document.GetSyntaxRootAsync(ct) is not SyntaxNode documentRoot)
                return document;

            //Check the node's leading and trailing trivia, the previous node's trailing trivia, and the next node's leading trivia.
            SyntaxToken token = documentRoot.FindToken(sourceSpan.Start);
            SyntaxToken previous = token.GetPreviousToken();
            SyntaxToken next = token.GetNextToken();

            SyntaxToken newToken = token;
            SyntaxToken newPrevious = previous;
            SyntaxToken newNext = next;

            //Attempt to solve on the token directly first.
            if (left > 0)
            {
                //Add to left of token.
                newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.Append(SyntaxFactory.Space));
            }
            else if (left < 0 && newToken.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                //Remove from left of token.
                newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.RemoveAt(newToken.LeadingTrivia.ToList().FindLastIndex(t => t.IsKind(SyntaxKind.WhitespaceTrivia))));
            }
            else if (left < 0 && newPrevious.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                //Remove from right of previous token.
                newPrevious = newPrevious.WithTrailingTrivia(newPrevious.TrailingTrivia.RemoveAt(newPrevious.TrailingTrivia.ToList().FindLastIndex(t => t.IsKind(SyntaxKind.WhitespaceTrivia))));
            }

            if (right > 0)
            {
                //Add to right of token.
                newToken = newToken.WithTrailingTrivia(newToken.TrailingTrivia.Insert(0, SyntaxFactory.Space));
            }
            else if (right < 0 && newToken.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                //Remove from right of token.
                newToken = newToken.WithTrailingTrivia(newToken.TrailingTrivia.RemoveAt(newToken.TrailingTrivia.ToList().FindIndex(t => t.IsKind(SyntaxKind.WhitespaceTrivia))));
            }
            else if (right < 0 && newNext.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                //Remove from left of next token.
                newNext = newNext.WithLeadingTrivia(newNext.LeadingTrivia.RemoveAt(newNext.LeadingTrivia.ToList().FindIndex(t => t.IsKind(SyntaxKind.WhitespaceTrivia))));
            }

            Dictionary<SyntaxToken, SyntaxToken> replacements = new()
            {
                { token, newToken },
                { previous, newPrevious },
                { next, newNext }
            };
            SyntaxNode newRoot = documentRoot.ReplaceTokens(replacements.Keys, (original, _) => replacements[original]);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
