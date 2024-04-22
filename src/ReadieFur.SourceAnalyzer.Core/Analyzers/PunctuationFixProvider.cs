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
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PunctuationAnalyzer.SpaceDiagnosticDescriptor.Id, PunctuationAnalyzer.NewLineDiagnosticDescriptor.Id);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == PunctuationAnalyzer.SpaceDiagnosticDescriptor.Id)
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: "Correct spacing",
                        createChangedDocument: ct => CorrectAround(
                            context.Document,
                            diagnostic.Location.SourceSpan,
                            diagnostic.Properties,
                            async _ => [SyntaxFactory.Space],
                            [SyntaxKind.WhitespaceTrivia],
                            ct),
                        equivalenceKey: "Punctuation"),
                        diagnostic);
                }
                else if (diagnostic.Id == PunctuationAnalyzer.NewLineDiagnosticDescriptor.Id)
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: "Correct new line",
                        createChangedDocument: ct => CorrectAround(
                            context.Document,
                            diagnostic.Location.SourceSpan,
                            diagnostic.Properties,
                            async isLeft =>
                            {
                                if (isLeft)
                                {
                                    int indentation = await Helpers.GetIndentationLevelForPositionAsync(
                                        await context.Document.GetSyntaxTreeAsync(ct),
                                        diagnostic.Location.GetLineSpan().StartLinePosition,
                                        ct);

                                    return [
                                        SyntaxFactory.LineFeed,
                                        SyntaxFactory.Whitespace(new string(' ', Helpers.IndentationLevelToSpaces(indentation)))
                                    ];
                                }
                                
                                return [SyntaxFactory.LineFeed];
                            },
                            [SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia],
                            ct),
                        equivalenceKey: "Punctuation"),
                        diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private async Task<Document> CorrectAround(
            Document document,
            TextSpan sourceSpan,
            ImmutableDictionary<string, string> properties,
            Func<bool, Task<IEnumerable<SyntaxTrivia>>> toInsert,
            SyntaxKind[] toRemove,
            CancellationToken ct)
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
                IEnumerable<SyntaxTrivia> syntaxToRemove = toRemove.Select(a => newPrevious.TrailingTrivia.ToList().FindLast(b => b.IsKind(a)));
                newPrevious = newPrevious.WithTrailingTrivia(newPrevious.TrailingTrivia.Where(t => !syntaxToRemove.Contains(t)));

                //Add to left of token.
                newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.AddRange(await toInsert(true)));
            }
            else if (left < 0 && newToken.LeadingTrivia.Any(t => toRemove.Contains(t.Kind())))
            {
                //Remove from left of token.
                IEnumerable<SyntaxTrivia> syntaxToRemove = toRemove.Select(a => newToken.LeadingTrivia.ToList().FindLast(b => b.IsKind(a)));
                newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.Where(t => !syntaxToRemove.Contains(t)));
            }
            else if (left < 0 && newPrevious.TrailingTrivia.Any(t => toRemove.Contains(t.Kind())))
            {
                //Remove from right of previous token.
                IEnumerable<SyntaxTrivia> syntaxToRemove = toRemove.Select(a => newPrevious.TrailingTrivia.ToList().FindLast(b => b.IsKind(a)));
                newPrevious = newPrevious.WithTrailingTrivia(newPrevious.TrailingTrivia.Where(t => !syntaxToRemove.Contains(t)));
            }

            if (right > 0)
            {
                IEnumerable<SyntaxTrivia> syntaxToRemove = toRemove.Select(a => newNext.LeadingTrivia.ToList().Find(b => b.IsKind(a)));
                newNext = newNext.WithLeadingTrivia(newNext.LeadingTrivia.Where(t => !syntaxToRemove.Contains(t)));

                //Add to right of token.
                newToken = newToken.WithTrailingTrivia(newToken.TrailingTrivia.InsertRange(0, await toInsert(true)));
            }
            else if (right < 0 && newToken.TrailingTrivia.Any(t => toRemove.Contains(t.Kind())))
            {
                //Remove from right of token.
                IEnumerable<SyntaxTrivia> syntaxToRemove = toRemove.Select(a => newToken.TrailingTrivia.ToList().Find(b => b.IsKind(a)));
                newToken = newToken.WithTrailingTrivia(newToken.TrailingTrivia.Where(t => !syntaxToRemove.Contains(t)));
            }
            else if (right < 0 && newNext.LeadingTrivia.Any(t => toRemove.Contains(t.Kind())))
            {
                //Remove from left of next token.
                IEnumerable<SyntaxTrivia> syntaxToRemove = toRemove.Select(a => newNext.LeadingTrivia.ToList().Find(b => b.IsKind(a)));
                newNext = newNext.WithLeadingTrivia(newNext.LeadingTrivia.Where(t => !syntaxToRemove.Contains(t)));
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
