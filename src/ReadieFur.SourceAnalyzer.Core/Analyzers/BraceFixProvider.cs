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
    internal class BraceFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(BraceAnalyzer.DiagnosticDescriptor.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not SyntaxNode documentRoot)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                        CodeAction.Create(
                        title: "Move brace to the " + (ConfigManager.Configuration.Formatting?.CurlyBraces?.NewLine ?? true ? "next" : "previous") + " line.",
                        //Use createChangedDocument as we only want to update the document in this case instead of providing a solution fix.
                        createChangedDocument: cancellationToken => MoveBraceAsync(context.Document, diagnostic.Location, cancellationToken),
                        equivalenceKey: "Move brace"),
                    diagnostic);
            }
        }

        private async Task<Document> MoveBraceAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxToken openBraceToken = root.FindToken(location.SourceSpan.Start);

            //We use the trivia object to determine where the brace should be moved.
            //The trivia object contains information about a given token such as its position, annotations, syntax, etc.
            //The trivia list contains a list of trivia objects that are associated with a given token.
            //The use of trivia objects is benificial over editing the document directly as it allows us to maintain the formatting of the document and to take a programmatic approach to editing the document via the syntax tree.

            Dictionary<SyntaxToken, SyntaxToken> updatedTokens = new(); //First token is the old token, second is the new token to be replaced with.
            if (ConfigManager.Configuration.Formatting?.CurlyBraces?.NewLine is true)
            {
                //Determine if the document is using CLRF, LF or CR.
                //For now I am going to use a simple check by only looking at the first new line.
                SyntaxTrivia lineEndingStyle = root.GetFirstToken().LeadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
                if (lineEndingStyle == default)
                {
                    //Default to CLRF if the style is unknown.
                    lineEndingStyle = SyntaxFactory.CarriageReturnLineFeed;
                }

                //Calculate indentation.
                //We can calculate the indentation required by traversing the tree to the current tokens line and determining the indentation of that line.
                //We can use this indentation as the new indentation for the new line as they should be the same.
                FileLinePositionSpan lineSpan = root.SyntaxTree.GetLineSpan(openBraceToken.Span);
                int indentation = 0;
                int indentationSize = 0;
                foreach (SyntaxToken token in root.DescendantTokens())
                {
                    //Limit search to everything prior to the target token.
                    if (token.Equals(openBraceToken))
                        break;

                    //TODO: Make this a binary search to possibly increase efficency? Might not be worth it though given the typical small number of tokens per line.
                    if (token.IsKind(SyntaxKind.OpenBraceToken))
                    {
                        indentation++;

                        //Calculate the indentation for later if nessecary.
                        if (indentationSize != 0)
                            continue;

                        //Get the first non-whitespace trivia for this line.
                        int tokenLine = root.SyntaxTree.GetLineSpan(token.Span).StartLinePosition.Line;
                        SyntaxToken tokenLineFirst = token;
                        while (root.SyntaxTree.GetLineSpan(tokenLineFirst.Span).StartLinePosition.Line == tokenLine)
                            tokenLineFirst = tokenLineFirst.GetPreviousToken();

                        //Find the whitespace trivia.
                        SyntaxTrivia leadingWhitespaceTrivia = tokenLineFirst.LeadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
                        if (leadingWhitespaceTrivia == default)
                            continue;

                        indentationSize = leadingWhitespaceTrivia.Span.Length;
                    }
                    else if (token.IsKind(SyntaxKind.CloseBraceToken))
                    {
                        indentation--;
                    }
                }

                //Calculate the indentation to be used.
                //Use the indentation from the user config if indentation settings are enabled, otherwise use the documents indentation (or fallback to the hardcoded default size).
                if (ConfigManager.Configuration.Formatting.Indentation is not null)
                    indentationSize = ConfigManager.Configuration.Formatting.Indentation.Size;
                else if (indentationSize == 0)
                    indentationSize = Indentation.DEFAULT_SIZE;
                //I prefer to use tabs for spacing, however I have not implimented a tab/space check yet, so for testing I will continue to use spaces.
                SyntaxTrivia indentationTrivia = SyntaxFactory.Whitespace(new string(' ', indentation * indentationSize));

                //Move the open brace to the next line by adding a new line to the leading trivia of the open brace token.
                SyntaxTrivia newLineTrivia = lineEndingStyle;

                //Update the leading trivia on the node, the newLineTrivia must come before the indentationTrivia otherwise the indention "won't" be applied.
                updatedTokens.Add(openBraceToken, openBraceToken.WithLeadingTrivia(openBraceToken.LeadingTrivia.AddRange([newLineTrivia, indentationTrivia])));

                //Strip any whitespace off of the end of the old line (not doing this causes the unit test to fail as the expected and outputs don't match due to the additinal left-over whitespace).
                SyntaxToken previousToken = openBraceToken.GetPreviousToken();
                if (previousToken.HasTrailingTrivia && previousToken.TrailingTrivia.Last().IsKind(SyntaxKind.WhitespaceTrivia))
                    updatedTokens.Add(previousToken, previousToken.WithTrailingTrivia(previousToken.TrailingTrivia.Take(previousToken.TrailingTrivia.Count - 1)));
            }
            else
            {
                List<SyntaxTrivia> leadingTrivia =
                [
                    //Move the open brace to the previous line by adding the leading trivia of the previous token to the open brace token.
                    .. openBraceToken.GetPreviousToken().TrailingTrivia,

                    //Remove all indentation and replace with a single space.
                    .. openBraceToken.LeadingTrivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)),
                ];
                
                updatedTokens.Add(openBraceToken, openBraceToken.WithLeadingTrivia(leadingTrivia));
            }

            //Batch update the tokens for efficency and consistency with the document changes.
            //The batch updater expects a callback to indicate what original tokens should be replaced with corrosponding trivia, I just use a simple dictionary lookup that contains the old and new tokens.
            SyntaxNode newRoot = root.ReplaceTokens(updatedTokens.Keys, (original, potentiallyModified) => updatedTokens[original]);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
