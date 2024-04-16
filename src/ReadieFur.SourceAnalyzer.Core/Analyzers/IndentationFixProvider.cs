using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using ReadieFur.SourceAnalyzer.Core.Configuration;
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

                context.RegisterCodeFix(
                        CodeAction.Create(
                        title: $"Set the indentation to {level} steps.",
                        //Use createChangedDocument (not a solution wide fix).
                        createChangedDocument: cancellationToken => CorrectIndentationAsync(context.Document, diagnostic.Location, levelInt, cancellationToken),
                        equivalenceKey: "Update indentation"),
                    diagnostic);
            }
        }

        private async Task<Document> CorrectIndentationAsync(Document document, Location location, int level, CancellationToken cancellationToken)
        {
            //Get the token.
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxToken token = root.FindToken(location.SourceSpan.Start);

            //Remove any existing whitespace and insert the corrected whitespace trivia.
            SyntaxTrivia[] newLeadingTrivia = [
                .. token.LeadingTrivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)),
                SyntaxFactory.Whitespace(new string(' ', ConfigManager.Configuration.Formatting.Indentation.Size * level))
            ];

            //Update the document.
            SyntaxToken updatedToken = token.WithLeadingTrivia(newLeadingTrivia);
            SyntaxNode newRoot = root.ReplaceToken(token, updatedToken);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
