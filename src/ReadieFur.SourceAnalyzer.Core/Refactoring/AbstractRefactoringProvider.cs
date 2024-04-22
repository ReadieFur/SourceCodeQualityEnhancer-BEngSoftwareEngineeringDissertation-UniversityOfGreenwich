using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Composition;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace ReadieFur.SourceAnalyzer.Core.Refactoring
{
    //https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.coderefactorings.coderefactoringprovider?view=roslyn-dotnet-4.1.0
    //[ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class AbstractRefactoringProvider// : CodeRefactoringProvider
    {
        /*public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            throw new NotImplementedException();
        }*/

        private readonly Project _project;

        public AbstractRefactoringProvider(Project project)
        {
            _project = project;
        }

        public async Task RefactorAsync()
        {
            await FindSimilarDocumentsAsync();
        }

        private async Task FindSimilarDocumentsAsync()
        {
            //Break down all documents into nodes.
            //Parallelize.
            List<Task<ImmutableArray<SyntaxNode>>> nodeTasks = new();
            foreach (Document document in _project.Documents)
                nodeTasks.Add(GetSyntaxNodesAsync(document));
            await Task.WhenAll(nodeTasks);
        }

        private async Task<ImmutableArray<SyntaxNode>> GetSyntaxNodesAsync(Document document)
        {
            //TODO: Check why these are returning empty.
            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            SyntaxNode root = await syntaxTree.GetRootAsync();
            return root.DescendantNodes(_ => true, true).ToImmutableArray();
        }
    }
}
