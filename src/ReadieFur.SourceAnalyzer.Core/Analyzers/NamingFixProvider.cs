using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Config;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class NamingFixProvider : CodeFixProvider
    {
        private static readonly Regex WELL_KNOWN_NAMING_SCHEME_PASCAL_CASE = new("^[A-Z][a-z]+(?:[A-Z][a-z]+)*$");
        private static readonly Regex WELL_KNOWN_NAMING_SCHEME_CAMEL_CASE = new("^[a-z]+(?:[A-Z][a-z]+)*$");

        private readonly IReadOnlyDictionary<string, NamingConvention> _descriptors = Helpers.GetNamingDescriptors().ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        public override ImmutableArray<string> FixableDiagnosticIds => _descriptors.Keys.ToImmutableArray();

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            //Attempt to provide a code fix in this method.
            SyntaxNode? node = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            if (node is null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                NamingConvention namingConvention = _descriptors[diagnostic.Id];

                //Trim the token of special characters from the front and end.
                TextSpan textSpan = diagnostic.Location.SourceSpan;
                var v = node.FindToken(textSpan.Start).Parent?.AncestorsAndSelf();
                continue;
            }
        }
    }
}
