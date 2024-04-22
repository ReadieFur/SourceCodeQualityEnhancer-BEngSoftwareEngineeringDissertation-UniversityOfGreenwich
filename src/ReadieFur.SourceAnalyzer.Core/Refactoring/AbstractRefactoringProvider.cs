using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Composition;
using System.Threading.Tasks;

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

        public async Task Refactor()
        {
        }

        private async Task FindSimilarDocuments()
        {
            foreach (Document document in _project.Documents)
            {
                //document.
            }
        }
    }
}
