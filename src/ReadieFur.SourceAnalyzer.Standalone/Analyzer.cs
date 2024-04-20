using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal class Analyzer
    {
        #region Static
        public static async Task<SolutionChanges> AnalyzeSolution(Solution solution)
        {
            Analyzer analyzer = new(solution);
            return await analyzer.AnalyzeSolutionAsync(solution);
        }
        #endregion

        private Solution _originalSolution;
        private Solution _solution;

        private Analyzer(Solution solution)
        {
            _originalSolution = solution;
            _solution = solution;
        }

        //https://johnkoerner.com/csharp/creating-a-stand-alone-code-analyzer/
        private async Task<SolutionChanges> AnalyzeSolutionAsync(Solution originalSolution)
        {
            //Get all diagnostic analyzers and code fix providers.
            ImmutableArray<DiagnosticAnalyzer> analyzers = GetAllAnalyzers();
            ImmutableArray<CodeFixProvider> codeFixProviders = GetAllCodeFixProviders();

            List<ProjectId> visitedProjectIDs = new();
            while (!visitedProjectIDs.SequenceEqual(_solution.ProjectIds))
            {
                Project project = _solution.GetProject(_solution.ProjectIds.FirstOrDefault(i => !visitedProjectIDs.Contains(i))) ?? throw new KeyNotFoundException();

                //Compile the project and analyze it with my diagnostics.
                Compilation? projectCompilation = await project.GetCompilationAsync();
                CompilationWithAnalyzers projectCompilationWithAnalyzers = projectCompilation.WithAnalyzers(analyzers);
                IEnumerable<Diagnostic> projectDiagnostics = (await projectCompilationWithAnalyzers.GetAllDiagnosticsAsync()).Where(d => d.Id.StartsWith(Core.Analyzers.Helpers.ANALYZER_ID_PREFIX));

                bool hasMadeChanges = false;
                foreach (Document? document in project.Documents)
                {
                    //Ignore auto-generated and library files in the obj directory.
                    if (document.FilePath.StartsWith(Directory.GetParent(project.FilePath).FullName)
                        && document.FilePath.Substring(Directory.GetParent(project.FilePath!).FullName.Length + 1).StartsWith("obj\\")) //+1 removes the leading backslash in the path.
                        continue;

                    Console.WriteLine("INFO: Analyzing document: " + document.FilePath);

                    //Get syntax tree for the document.
                    SyntaxTree documentSyntaxTree = await document.GetSyntaxTreeAsync();

                    //Get the diagnostics related to this document.
                    IEnumerable<Diagnostic> documentDiagnostics = projectDiagnostics.Where(d => d.Location.SourceTree.IsEquivalentTo(documentSyntaxTree));

                    //Apply code fixes for reported diagnostics.
                    hasMadeChanges = await ApplyCodeFixesAsync(document, documentDiagnostics.ToImmutableArray(), codeFixProviders);

                    if (hasMadeChanges)
                        break;
                }

                //If changes have been made to the workspace then decrement the counter and re-evaluate the solution.
                if (hasMadeChanges)
                {
                    Console.WriteLine("INFO: Re-evaluate.");
                    continue;
                }

                visitedProjectIDs.Add(project.Id);
            }

            //TODO: Ask the user if they would like to apply the changes in-place or save the modified solution to a new location OR show a diff UI with the changes made.
            SolutionChanges solutionChanges = _solution.GetChanges(originalSolution);
            //workspace.TryApplyChanges(solution);
            return solutionChanges;
        }

        private static ImmutableArray<DiagnosticAnalyzer> GetAllAnalyzers()
        {
            List<DiagnosticAnalyzer> analyzers = new();

            //https://stackoverflow.com/questions/1914058/how-do-i-get-a-list-of-all-loaded-types-in-c
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (string.IsNullOrEmpty(type.Namespace)
                    || !type.Namespace.StartsWith("ReadieFur.SourceAnalyzer.Core")
                    || type.IsAbstract
                    || !typeof(DiagnosticAnalyzer).IsAssignableFrom(type)
                    || type.GetCustomAttribute<DiagnosticAnalyzerAttribute>() is not DiagnosticAnalyzerAttribute attribute
                    || !attribute.Languages.Contains(LanguageNames.CSharp))
                    continue;

                analyzers.Add((DiagnosticAnalyzer)Activator.CreateInstance(type));
            }

            return analyzers.ToImmutableArray();
        }

        private static ImmutableArray<CodeFixProvider> GetAllCodeFixProviders()
        {
            List<CodeFixProvider> codeFixProviders = new();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (string.IsNullOrEmpty(type.Namespace)
                    || !type.Namespace.StartsWith("ReadieFur.SourceAnalyzer.Core")
                    || type.IsAbstract
                    || !typeof(CodeFixProvider).IsAssignableFrom(type)
                    || type.GetCustomAttribute<ExportCodeFixProviderAttribute>() is not ExportCodeFixProviderAttribute attribute
                    || !attribute.Languages.Contains(LanguageNames.CSharp))
                    continue;

                codeFixProviders.Add((CodeFixProvider)Activator.CreateInstance(type));
            }

            return codeFixProviders.ToImmutableArray();
        }

        //Ref cannot be used here so I must use a static class reference instead.
        private async Task<bool> ApplyCodeFixesAsync(/*ref Solution solution,*/ Document document, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<CodeFixProvider> codeFixProviders)
        {
            //TODO: Pass this as a variable.
            CancellationTokenSource cts = new();

            foreach (Diagnostic diagnostic in diagnostics)
            {
                foreach (CodeFixProvider codeFixProvider in codeFixProviders)
                {
                    if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                        continue;

                    List<CodeAction> actions = new();
                    var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), cts.Token);
                    await codeFixProvider.RegisterCodeFixesAsync(context);

                    //There "should" only ever be 1 action here, but attempt to solve for multiple IF multiple do appear.
                    foreach (CodeAction action in actions)
                    {
                        ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(cts.Token);

                        foreach (CodeActionOperation operation in operations)
                        {
                            if (operation is not ApplyChangesOperation applyChangesOperation)
                                continue;

                            //Check if any changes have actually been made.
                            SolutionChanges solutionChanges = applyChangesOperation.ChangedSolution.GetChanges(_solution);
                            foreach (ProjectChanges projectChanges in solutionChanges.GetProjectChanges())
                            {
                                foreach (DocumentId documentID in projectChanges.GetChangedDocuments())
                                {
                                    IEnumerable<TextChange> textChanges = await applyChangesOperation.ChangedSolution.GetDocument(documentID)!.GetTextChangesAsync(document);
                                    if (textChanges.Any())
                                    {
                                        //At least one change was detected and so we should apply and re-evaluate the solution.
                                        Console.WriteLine($"INFO: Applying change at {diagnostic.Location.SourceSpan}: {diagnostic.GetMessage()}");
                                        //applyChangesOperation.Apply(solution.Workspace, cts.Token);
                                        _solution = applyChangesOperation.ChangedSolution;
                                        var a = (await _solution.GetDocument(documentID)!.GetTextAsync()).ToString();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
