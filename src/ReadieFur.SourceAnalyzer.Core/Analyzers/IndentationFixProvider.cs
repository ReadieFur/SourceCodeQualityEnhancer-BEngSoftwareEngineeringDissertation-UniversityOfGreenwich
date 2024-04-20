using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class IndentationFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IndentationAnalyzer.DiagnosticDescriptor.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not SyntaxNode documentRoot)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (!diagnostic.Properties.TryGetValue("level", out string level) || string.IsNullOrEmpty(level)
                    || !int.TryParse(level, out int levelInt) || levelInt < 0)
                    continue;

                if (!diagnostic.Properties.TryGetValue("diagnosticType", out string diagnosticType) || string.IsNullOrEmpty(diagnosticType))
                    continue;

                context.RegisterCodeFix(
                        CodeAction.Create(
                        title: $"Set the indentation to {level} steps.",
                        //Use createChangedDocument (not a solution wide fix).
                        createChangedDocument: cancellationToken => CorrectIndentationAsync(context.Document, diagnostic.Location, levelInt, diagnosticType, cancellationToken),
                        equivalenceKey: "Update indentation"),
                    diagnostic);
            }
        }

        private async Task<Document> CorrectIndentationAsync(Document document, Location location, int level, string diagnosticType, CancellationToken cancellationToken)
        {
            //Get the token.
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

            SyntaxTrivia whitespaceTrivia = SyntaxFactory.Whitespace(new string(' ', ConfigManager.Configuration.Formatting.Indentation.Size * level));

            SyntaxToken token;
            SyntaxTrivia? trivia = null;
            switch (diagnosticType)
            {
                case nameof(SyntaxNode):
                    token = root.FindNode(location.SourceSpan).FindToken(location.SourceSpan.Start);
                    break;
                case nameof(SyntaxToken):
                    token = root.FindToken(location.SourceSpan.Start);
                    break;
                case nameof(SyntaxTrivia):
                    {
                        trivia = root.FindTrivia(location.SourceSpan.Start, true);
                        token = trivia.Value.Token;

                        if (token.LeadingTrivia.Contains(trivia.Value))
                            break;

                        //If the token is a trailing trivia then run a different update operation to the code below (this shouldn't occur I don't believe).
                        //TODO: UNTESTED.
                        if (token.TrailingTrivia.Contains(trivia.Value))
                        {
                            int triviaIndex = token.TrailingTrivia.IndexOf(trivia.Value);
                            SyntaxTriviaList _newLeadingTrivia = token.TrailingTrivia.Insert(triviaIndex, whitespaceTrivia);

                            //Update the document.
                            SyntaxToken _updatedToken = token.WithLeadingTrivia(_newLeadingTrivia);
                            SyntaxNode _newRoot = root.ReplaceToken(token, _updatedToken);
                            return document.WithSyntaxRoot(_newRoot);
                        }
                        break;
                    }
                default:
                    //Should not occur.
                    return document;
            }

            //Remove any existing whitespace and insert the corrected whitespace trivia, UNLESS we are working on a trivia token that is infront of the EOL trivia, in which case we modify before the EOL trivia.
            List<SyntaxTrivia> newLeadingTrivia = new();
            int eolTriviaIndex = token.LeadingTrivia.IndexOf(token.LeadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)));
            if (eolTriviaIndex != -1 && trivia.HasValue && token.LeadingTrivia.Contains(trivia.Value))
            {
                //In this case we need to place the whitespace as the very first thing because we are working on a leading trivia value which has a parent token on the NEXT line.
                newLeadingTrivia.Add(whitespaceTrivia);
                newLeadingTrivia.AddRange(token.LeadingTrivia.Take(eolTriviaIndex + 1).Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)));
                newLeadingTrivia.AddRange(token.LeadingTrivia.Skip(newLeadingTrivia.Count));
            }
            else if (eolTriviaIndex != -1)
            {
                //If an end of line character was found in the leading trivia, keep everything before the EOL triva intact as this trivia will be on a line different from the one we want to modify.
                newLeadingTrivia.AddRange(token.LeadingTrivia.Take(eolTriviaIndex + 1));
                //After the EOL trivia we can insert out modified trivia which includes the new whitespace and everything after the EOL trivia that isn't whitespace.
                newLeadingTrivia.Add(whitespaceTrivia);
                newLeadingTrivia.AddRange(token.LeadingTrivia.Skip(newLeadingTrivia.Count).Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)));
            }
            else
            {
                newLeadingTrivia.Add(whitespaceTrivia);
                newLeadingTrivia.AddRange(token.LeadingTrivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)));
            }

            //Update the document.
            SyntaxToken updatedToken = token.WithLeadingTrivia(newLeadingTrivia);
            SyntaxNode newRoot = root.ReplaceToken(token, updatedToken);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
