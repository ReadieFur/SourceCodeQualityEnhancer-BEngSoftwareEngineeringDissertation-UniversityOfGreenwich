using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Rename;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers.Naming
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassName)), Shared]
    internal class ClassName : ANamingProvider
    {
        protected override string Title => "ClassName";
        protected override string ToolTip => "Make uppercase";
        protected override string MessageFormat => "Type name '{0}' contains lowercase letters";
        protected override ECategory Category => ECategory.Naming;
        protected override DiagnosticSeverity Severity => DiagnosticSeverity.Warning;
        protected override bool IsEnabledByDefault => true;
        protected override string Description => "Type name should be in PascalCase";

        public override void Analyze(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol namedTypeSymbol
                && namedTypeSymbol.TypeKind == TypeKind.Class
                && char.IsLower(namedTypeSymbol.Name[0]))
                context.ReportDiagnostic(Diagnostic.Create(descriptor, namedTypeSymbol.Locations[0], namedTypeSymbol.Name));
        }

        protected override async Task<Solution> CreateSolution(Document document, TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
        {
            //Get the symbol representing the type to be renamed.
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            ISymbol? typeSymbol = semanticModel?.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            if (typeSymbol == null)
                //Return the original solution if we can't get the symbol.
                return document.Project.Solution;

            //Produce a new solution that has all references to that type renamed, including the declaration.
#pragma warning disable CS0618 // Type or member is obsolete
            //I can't seem to find a replacement for this method at this time.
            return await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                typeSymbol,
                typeDeclaration.Identifier.Text.ToUpperInvariant(),
                document.Project.Solution.Workspace.Options,
                cancellationToken
            ).ConfigureAwait(false);
#pragma warning restore CS0618
        }
    }
}
