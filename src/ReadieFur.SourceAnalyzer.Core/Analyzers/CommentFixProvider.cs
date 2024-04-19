using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CommentFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            CommentAnalyzer.LeadingSpaceDiagnosticDescriptor.Id,
            CommentAnalyzer.TrailingFullStopDiagnosticDescriptor.Id,
            CommentAnalyzer.NewLineDiagnosticDescriptor.Id
        );

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not SyntaxNode documentRoot)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == CommentAnalyzer.LeadingSpaceDiagnosticDescriptor.Id)
                    context.RegisterCodeFix(CodeAction.Create(
                            title: (ConfigManager.Configuration.Formatting.Comments.LeadingSpace ? "Add a" : "Remove the") + " space after the comment decleration.",
                            createChangedDocument: cancellationToken => CorrectLeadingSpaceAsync(context.Document, diagnostic.Location, cancellationToken),
                            equivalenceKey: "Format comment"),
                        diagnostic);
                else if (diagnostic.Id == CommentAnalyzer.TrailingFullStopDiagnosticDescriptor.Id)
                    context.RegisterCodeFix(CodeAction.Create(
                            title: (ConfigManager.Configuration.Formatting.Comments.LeadingSpace ? "Add a" : "Remove the") + " full stop end of the comment.",
                            createChangedDocument: cancellationToken => CorrectTrailingFullStopAsync(context.Document, diagnostic.Location, cancellationToken),
                            equivalenceKey: "Format comment"),
                        diagnostic);
                else if (diagnostic.Id == CommentAnalyzer.NewLineDiagnosticDescriptor.Id)
                    context.RegisterCodeFix(CodeAction.Create(
                            title: ConfigManager.Configuration.Formatting.Comments.NewLine ? "Move the comment to a new line." : "Move the comment to the same line as the owning token.",
                            createChangedDocument: cancellationToken => CorrectCommentLineAsync(context.Document, diagnostic.Location, cancellationToken),
                            equivalenceKey: "Format comment"),
                        diagnostic);
            }
        }

        private async Task<Document> CorrectLeadingSpaceAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            //Get the trivia.
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxTrivia trivia = root.FindTrivia(location.SourceSpan.Start);

            SyntaxTrivia updatedTrivia;
            switch (trivia.RawKind)
            {
                case (int)SyntaxKind.SingleLineCommentTrivia:
                    updatedTrivia = SyntaxFactory.Comment(ConfigManager.Configuration.Formatting.Comments.LeadingSpace ? "// " + trivia.ToString().Substring(2) : "//" + trivia.ToString().Substring(3));
                    break;
                default:
                    return document;
            }

            //Update the document.
            SyntaxNode newRoot = root.ReplaceTrivia(trivia, updatedTrivia);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> CorrectTrailingFullStopAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxTrivia trivia = root.FindTrivia(location.SourceSpan.Start);

            SyntaxTrivia updatedTrivia;
            switch (trivia.RawKind)
            {
                case (int)SyntaxKind.SingleLineCommentTrivia:
                    updatedTrivia = SyntaxFactory.Comment(ConfigManager.Configuration.Formatting.Comments.TrailingFullStop ? trivia.ToString() + "." : trivia.ToString().TrimEnd(' ', '.'));
                    break;
                default:
                    return document;
            }

            SyntaxNode newRoot = root.ReplaceTrivia(trivia, updatedTrivia);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> CorrectCommentLineAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxTrivia commentTrivia = root.FindTrivia(location.SourceSpan.Start);
            Dictionary<SyntaxToken, SyntaxToken> updatedTokens = new();

            if (commentTrivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                FileLinePositionSpan lineSpan = root.SyntaxTree.GetLineSpan(commentTrivia.Span);
                bool isCommentInTrailingTrivia = commentTrivia.Token.TrailingTrivia.Contains(commentTrivia);

                if (ConfigManager.Configuration.Formatting.Comments.NewLine)
                {
                    //Determine if the document is using CLRF, LF or CR.
                    SyntaxTrivia lineEndingStyle = root.GetFirstToken().LeadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
                    if (lineEndingStyle == default)
                        lineEndingStyle = SyntaxFactory.CarriageReturnLineFeed; //Default to CLRF if the style is unknown.

                    //Get the previous line, this is where the comment will inserted at.
                    SyntaxToken lastTokenOnPreviousLine = commentTrivia.Token;
                    SyntaxToken firstToken = root.GetFirstToken();
                    while (!lastTokenOnPreviousLine.Equals(firstToken) && lastTokenOnPreviousLine.GetLocation().GetLineSpan().StartLinePosition.Line == lineSpan.StartLinePosition.Line)
                        lastTokenOnPreviousLine = lastTokenOnPreviousLine.GetPreviousToken();

                    //Calculate indentation.
                    int indentation = lastTokenOnPreviousLine.GetNextToken().LeadingTrivia.First(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString().Length;
                    SyntaxTrivia indentationTrivia = SyntaxFactory.Whitespace(new string(' ', indentation));

                    //Update the leading trivia on the owning token such that it is lead with: new line, comment, new line, owning token leading trivia, owning token.
                    List<SyntaxTrivia> previousLineTrailingTrivia =
                    [
                        .. lastTokenOnPreviousLine.TrailingTrivia,
                        indentationTrivia,
                        commentTrivia,
                        lineEndingStyle,
                    ];
                    updatedTokens.Add(lastTokenOnPreviousLine, lastTokenOnPreviousLine.WithTrailingTrivia(previousLineTrailingTrivia));

                    //Remove the comment from its old position (most likley on trailing trivia so check that first).
                    if (isCommentInTrailingTrivia)
                    {
                        //Making sure to remove trailing whitespace from the token.
                        updatedTokens.Add(commentTrivia.Token, commentTrivia.Token.WithTrailingTrivia(commentTrivia.Token.TrailingTrivia.Where(t => t != commentTrivia && !t.IsKind(SyntaxKind.WhitespaceTrivia))));
                    }
                    else
                    {
                        updatedTokens.Add(commentTrivia.Token, commentTrivia.Token.WithLeadingTrivia(commentTrivia.Token.LeadingTrivia.Where(t => t != commentTrivia)));
                    }
                }
                else
                {
                    //Get the end of the next line, this is where the comment will be placed.
                    SyntaxToken lastTokenOnNextLine = commentTrivia.Token;
                    SyntaxToken lastToken = root.GetLastToken();
                    while (!lastTokenOnNextLine.Equals(lastToken) && lastTokenOnNextLine.GetLocation().GetLineSpan().StartLinePosition.Line <= lineSpan.StartLinePosition.Line + 1)
                        lastTokenOnNextLine = lastTokenOnNextLine.GetNextToken();
                    //TODO: Add a whitespace after the token and before the comment.
                    updatedTokens.Add(lastTokenOnNextLine, lastTokenOnNextLine.WithTrailingTrivia(lastTokenOnNextLine.TrailingTrivia.Prepend(commentTrivia)));

                    //Comments that are to be moved to be on the same line as code typically are above the code that is being referenced, so we need to move the token to be a trailing token of the next line.
                    SyntaxTrivia commentEOLTrivia = isCommentInTrailingTrivia
                        ? commentTrivia.Token.TrailingTrivia.SkipWhile(t => t != commentTrivia).First(t => t.IsKind(SyntaxKind.EndOfLineTrivia))
                        : commentEOLTrivia = commentTrivia.Token.LeadingTrivia.SkipWhile(t => t != commentTrivia).First(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

                    updatedTokens.Add(commentTrivia.Token, commentTrivia.Token.WithLeadingTrivia(
                        (isCommentInTrailingTrivia ? commentTrivia.Token.TrailingTrivia : commentTrivia.Token.LeadingTrivia).Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) || t.Equals(commentEOLTrivia))));
                }
            }

            //Batch update the tokens for efficency and consistency with the document changes.
            SyntaxNode newRoot = root.ReplaceTokens(updatedTokens.Keys, (original, potentiallyModified) => updatedTokens[original]);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
