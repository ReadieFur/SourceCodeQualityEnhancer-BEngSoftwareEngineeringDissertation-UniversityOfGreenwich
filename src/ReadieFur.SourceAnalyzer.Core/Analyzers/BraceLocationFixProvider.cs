using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Collections.Immutable;
using System.Composition;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class BraceLocationFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(BraceLocationAnalyzer.DiagnosticDescriptor.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not SyntaxNode documentRoot)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                        CodeAction.Create(
                        title: "Move brace to the " + (ConfigManager.Configuration.Formatting.CurlyBraces.NewLine ? "next" : "previous") + " line.",
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

            SyntaxToken newOpenBraceToken = ConfigManager.Configuration.Formatting.CurlyBraces.NewLine
                //Move the open brace to the next line by adding a new line to the leading trivia of the open brace token.
                ? openBraceToken.WithLeadingTrivia(openBraceToken.LeadingTrivia.Add(SyntaxFactory.CarriageReturnLineFeed))
                //Move the open brace to the previous line by adding the leading trivia of the previous token to the open brace token.
                : openBraceToken.WithLeadingTrivia(openBraceToken.LeadingTrivia.AddRange(openBraceToken.GetPreviousToken().TrailingTrivia));

            SyntaxNode newRoot = root.ReplaceToken(openBraceToken, newOpenBraceToken);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
