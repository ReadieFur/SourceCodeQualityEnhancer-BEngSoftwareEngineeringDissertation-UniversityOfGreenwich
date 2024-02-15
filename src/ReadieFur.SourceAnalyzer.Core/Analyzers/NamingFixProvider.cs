using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
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
    public class NamingFixProvider : CodeFixProvider
    {
        private readonly IReadOnlyDictionary<string, NamingConvention> _descriptors = Helpers.GetNamingDescriptors().ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        public override ImmutableArray<string> FixableDiagnosticIds => _descriptors.Keys.ToImmutableArray();

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            //Attempt to provide a code fix in this method.
            SyntaxNode? documentRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (documentRoot is null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                NamingConvention namingConvention = _descriptors[diagnostic.Id];

                //Get the declaration node that the diagnostic is associated with.
                //Reference: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix
                SyntaxNode? declaration = documentRoot
                    .FindToken(diagnostic.Location.SourceSpan.Start) //Get the token specified by the diagnostic location.
                    .Parent? //Get the parent of the token.
                    .AncestorsAndSelf() //This provides a "tree" of the source file contents from the root to the node that contains the specified token.
                    .First(); //We want the first ancestor (i.e. the token node).

                //If the declaration is null, we can't provide a code fix (shouldn't happen).
                if (declaration is null)
                    continue;

                /* As a general rule, analyzers should exit as quickly as possible, doing minimal work. Visual Studio calls registered analyzers as the user edits code. Responsiveness is a key requirement.
                 * See: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix#create-tests-for-valid-declarations
                 */
                /* Due to the above rule, it will be difficult for us to provide a code fix for every diagnostic as we need to dynamically analyze if it is even possible with the provided information.
                 * It becomes difficult because we need to process the data in a way that is not immediately available and so it goes against the rule of being responsive and arguably Regex is relatively slow.
                 */
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Rename to match: {namingConvention.Pattern}",
                        createChangedSolution: ct => RenameSymbolAsync(context.Document, declaration, namingConvention, ct),
                        equivalenceKey: namingConvention.Pattern
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Solution> RenameSymbolAsync(Document document, SyntaxNode node, NamingConvention namingConvention, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
