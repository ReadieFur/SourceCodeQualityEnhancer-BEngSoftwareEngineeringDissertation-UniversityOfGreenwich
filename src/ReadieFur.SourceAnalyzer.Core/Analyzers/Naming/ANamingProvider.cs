using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers.Naming
{
    internal abstract class ANamingProvider : CodeFixProvider
    {
        protected abstract string Title { get; }
        protected abstract string ToolTip { get; }
        protected abstract string MessageFormat { get; }
        protected abstract ECategory Category { get; }
        protected abstract DiagnosticSeverity Severity { get; }
        protected abstract bool IsEnabledByDefault { get; }
        protected abstract string Description { get; }
        public DiagnosticDescriptor descriptor => new(GetType().FullName, Title, MessageFormat, Category.ToString(), Severity, IsEnabledByDefault, Description);
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(GetType().FullName);

        protected abstract Task<Solution> CreateSolution(Document document, TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken);
        public abstract void Analyze(SymbolAnalysisContext context);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            //As far as I am aware, due to my use case I don't need to check for the diagnostic constraints here as the analyzer will only report diagnostics that are fixable by this code fix provider (via the Analyze method).

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                TypeDeclarationSyntax? typeDeclaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                if (typeDeclaration == null)
                    continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: ToolTip,
                        createChangedSolution: ct => CreateSolution(context.Document, typeDeclaration, ct)),
                    diagnostic);
            }
        }
    }
}
