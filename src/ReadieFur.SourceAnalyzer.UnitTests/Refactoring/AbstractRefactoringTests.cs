using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using ReadieFur.SourceAnalyzer.Core.Refactoring;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ReadieFur.SourceAnalyzer.UnitTests.Refactoring
{
    [CTest]
    public class AbstractRefactoringTests
    {
        private static async Task Wrapper(string[] files, Func<Project, Task> func)
        {
            MSBuildWorkspace? workspace = null;
            try
            {
                //Setup MSBuild.
                //For some reason MSBuildLocator wasn't working from within this test project so I will locate it manually for now.
                MSBuildLocator.RegisterMSBuildPath("S:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin");
                workspace = MSBuildWorkspace.Create();

                Solution solution = workspace.CurrentSolution;

                //Load files.
                string basePath = GetSolutionPath() ?? throw new Exception("Could not find solution directory.");
                IEnumerable<string> sourceFilePaths = files.Select(file => FindFile(basePath, file) ?? throw new Exception($"Could not find source file: {file}"));

                //Create a temporary project to test the refactoring.
                ProjectId projectId = ProjectId.CreateNewId();
                ProjectInfo projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "TestProject", "TestProject", LanguageNames.CSharp)
                    .WithMetadataReferences(new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }) //Load default references.
                    .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)) //Set output type to be a dll (so a static entrypoint is not required).
                    .WithParseOptions(new CSharpParseOptions(LanguageVersion.Latest)) //Set the language version to the latest.
                    .WithDocuments(sourceFilePaths.Select(filePath => DocumentInfo.Create(DocumentId.CreateNewId(projectId), filePath, filePath: filePath)));
                solution = solution.AddProject(projectInfo);

                //Get the project.
                Project project = solution.GetProject(projectId) ?? throw new Exception("Could not find project.");

                //Precompile to ensure there are no errors.
                await Compile(project);

                await func(project);

                //Compile again to make sure we didn't not break anything.
                await Compile(project);
            }
            finally
            {
                if (workspace is not null)
                    workspace.Dispose();
                MSBuildLocator.Unregister();
            }
        }

        private static async Task Compile(Project project)
        {
            Compilation compilation = await project.GetCompilationAsync() ?? throw new Exception("Could not compile project.");
            if (compilation.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                Assert.Fail("Project has errors:" + string.Join(Environment.NewLine,
                    compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.GetMessage())));
            }
        }

        [MTest]
        public async Task TestAsync() => await Wrapper(["AbstractRefactoring_A.cs", "AbstractRefactoring_B.cs"], TestAsync);
        private async Task TestAsync(Project project)
        {
            AbstractRefactoringProvider refactoringProvider = new(project);
            await refactoringProvider.RefactorAsync();
        }
    }
}
