using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CommentFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CommentAnalyzer.DiagnosticDescriptor.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not SyntaxNode documentRoot)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                        CodeAction.Create(
                        title: (ConfigManager.Configuration.Formatting.Comments.AddSpaceBeforeComment ? "Add a" : "Remove the") + " space after the comment decleration.",
                        createChangedDocument: cancellationToken => CorrectPrefixAsync(context.Document, diagnostic.Location, cancellationToken),
                        equivalenceKey: "Format comment"),
                    diagnostic);
            }
        }

        private async Task<Document> CorrectPrefixAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            //Get the trivia.
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxTrivia trivia = root.FindTrivia(location.SourceSpan.Start);

            SyntaxTrivia updatedTrivia;
            switch (trivia.RawKind)
            {
                case (int)SyntaxKind.SingleLineCommentTrivia:
                    updatedTrivia = SyntaxFactory.Comment("//" + trivia.ToString().Substring(3));
                    break;
                default:
                    return document;
            }

            //Update the document.
            SyntaxNode newRoot = root.ReplaceTrivia(trivia, updatedTrivia);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
