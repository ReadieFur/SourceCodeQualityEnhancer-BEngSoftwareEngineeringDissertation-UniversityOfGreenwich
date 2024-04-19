﻿using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Document = Microsoft.CodeAnalysis.Document;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //Attempt to set the version of MSBuild.
            VisualStudioInstance[] visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            VisualStudioInstance instance = visualStudioInstances.Length == 1
                //If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                //Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            /* NOTE: Be sure to register an instance with the MSBuildLocator 
             * before calling MSBuildWorkspace.Create()
             * otherwise, MSBuildWorkspace won't MEF compose.
            */
            MSBuildLocator.RegisterInstance(instance);

            using (MSBuildWorkspace? workspace = MSBuildWorkspace.Create())
            {
                //Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (_, e) =>
                {
                    //Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: " + e.Diagnostic.Message);
                    //Console.ForegroundColor = ConsoleColor.Gray;
                    //Environment.Exit(1);
                };

                //Get a solution to load from the user.
                string path =
                    args.Length > 0 && (args[0].EndsWith(".sln") || args[0].EndsWith(".csproj")) && File.Exists(args[0])
                    ? args[0]
                    : GetSolution();
                Console.WriteLine($"Loading solution '{path}'");

                //Attach progress reporter so we print projects as they are loaded.
                Solution solution;
                if (path.EndsWith(".sln"))
                {
                    solution = await workspace.OpenSolutionAsync(path, new ConsoleProgressReporter());
                    Console.WriteLine($"Finished loading solution '{path}'");
                }
                else
                {
                    solution = (await workspace.OpenProjectAsync(path, new ConsoleProgressReporter())).Solution;
                    Console.WriteLine($"Finished loading project '{path}'");
                }

                //Load an analyzer configuration file.
                await LoadAnalyzerConfigurationAsync(args, solution);

                //Perform analysis on the projects in the loaded solution.
                await AnalyzeSolutionAsync(solution);
            }
        }

        private static async Task LoadAnalyzerConfigurationAsync(string[] args, Solution solution)
        {
            if (args.Length > 1 && args[1] == "default")
            {
                Console.WriteLine("INFO: Using default analyzer configuration settings.");
                ConfigManager.Instance.LoadDefaultConfiguration();
                return;
            }
            else if (args.Length > 1 && args[1].EndsWith("source-analyzer.yaml") && File.Exists(args[1]) && await ConfigManager.Instance.LoadAsync(args[1]))
            {
                Console.WriteLine("INFO: Using analyzer configuration from file: " + args[1]);
                return;
            }

            bool analyzerConfigLoaded = false;
            foreach (Project? project in solution.Projects)
            {
                foreach (Document? document in project.Documents)
                {
                    if (!document.Name.EndsWith("source-analyzer.yaml"))
                        continue;

                    analyzerConfigLoaded = await ConfigManager.Instance.LoadAsync(document.FilePath);

                    if (analyzerConfigLoaded)
                    {
                        Console.WriteLine("INFO: Using analyzer configuration from file: " + document.FilePath);
                        break;
                    }
                }

                if (analyzerConfigLoaded)
                    break;
            }
            if (!analyzerConfigLoaded)
            {
#if RELEASE || (DEBUG && true)
                Console.WriteLine("No analyzer configuration file found! Please provide the full path to an analyzer configuration file or leave blank to load defaults:");

                while (true)
                {
                    string input = Console.ReadLine().Trim();
                    if (string.IsNullOrEmpty(input))
                    {
                        ConfigManager.Instance.LoadDefaultConfiguration();
                        return;
                    }

                    if (input.EndsWith(".source-analyzer.yaml") && !File.Exists(input) && !await ConfigManager.Instance.LoadAsync(input))
                    {
                        Console.WriteLine("INFO: Using analyzer configuration from file: " + input);
                        return;
                    }
                 
                    Console.WriteLine("Input not accepted, try again.");
                }
#else
                Console.WriteLine("WARN: No analyzer configuration found! Using default configuration.");
                ConfigManager.Instance.LoadDefaultConfiguration();
#endif
            }
        }

        //https://johnkoerner.com/csharp/creating-a-stand-alone-code-analyzer/
        private static async Task AnalyzeSolutionAsync(Solution solution)
        {
            //Get all diagnostic analyzers and code fix providers.
            ImmutableArray<DiagnosticAnalyzer> analyzers = GetAllAnalyzers();
            ImmutableArray<CodeFixProvider> codeFixProviders = GetAllCodeFixProviders();
            
            List<SolutionChanges> solutionChanges = new();

            foreach (Project? project in solution.Projects)
            {
                //Compile the project and analyze it with my diagnostics.
                Compilation? projectCompilation = await project.GetCompilationAsync();
                CompilationWithAnalyzers projectCompilationWithAnalyzers = projectCompilation.WithAnalyzers(analyzers);
                IEnumerable<Diagnostic> projectDiagnostics = (await projectCompilationWithAnalyzers.GetAllDiagnosticsAsync()).Where(d => d.Id.StartsWith(Core.Analyzers.Helpers.ANALYZER_ID_PREFIX));

                foreach (Document? document in project.Documents)
                {
                    //Get syntax tree for the document.
                    SyntaxTree documentSyntaxTree = await document.GetSyntaxTreeAsync();

                    //Get the diagnostics related to this document.
                    IEnumerable<Diagnostic> documentDiagnostics = projectDiagnostics.Where(d => d.Location.SourceTree.IsEquivalentTo(documentSyntaxTree));

                    //Apply code fixes for reported diagnostics.
                    solutionChanges.AddRange(await GetSolutionCodeFixesAsync(solution, document, documentDiagnostics.ToImmutableArray(), codeFixProviders));
                }
            }

            /*//Attempt to merge all of the changes and resolve any conflicts.
            foreach (SolutionChanges updatedSolution in solutionChanges)
            {
            }*/
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

        private static async Task<List<SolutionChanges>> GetSolutionCodeFixesAsync(Solution solution, Document document, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<CodeFixProvider> codeFixProviders)
        {
            //TODO: Pass this as a variable.
            CancellationTokenSource cts = new();
            List<SolutionChanges> solutionChanges = new();

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

                            solutionChanges.Add(applyChangesOperation.ChangedSolution.GetChanges(solution));
                        }
                    }
                }
            }

            return solutionChanges;
        }

        private static string GetSolution()
        {
            //Attempt to find a project file in the current directory.
            if (FindSolutionFile(Environment.CurrentDirectory, out string? cwdSolutionFile) && cwdSolutionFile is not null)
                return cwdSolutionFile;

            //Otherwise prompt the user for a file.
            Console.WriteLine("Please enter the path to the file or directory for the C# project or solution you would like to analyze:");
            while (true)
            {
                string input = Console.ReadLine().Trim();

                if ((input.EndsWith(".sln") || input.EndsWith(".csproj")) && File.Exists(input))
                    return input;
                else if (Directory.Exists(input) && FindSolutionFile(input, out string? discoveredSolutionFile) && discoveredSolutionFile is not null)
                    return discoveredSolutionFile;

                Console.WriteLine("Input not accepted, try again.");
            }
        }

        private static bool FindSolutionFile(string path, out string? solutionFile)
        {
            solutionFile = default;

            string[] files = Directory.GetFiles(path);
            solutionFile = files.FirstOrDefault(f => f.EndsWith(".sln"));
            if (solutionFile != default)
                return true;
            solutionFile = files.FirstOrDefault(f => f.EndsWith(".csproj"));
            if (solutionFile != default)
                return true;

            return false;
        }

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"\tName: {visualStudioInstances[i].Name}");
                Console.WriteLine($"\tVersion: {visualStudioInstances[i].Version}");
                Console.WriteLine($"\tMSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

#if DEBUG
            return visualStudioInstances[1];
#else
            while (true)
            {
                string userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                    return visualStudioInstances[instanceNumber - 1];
                Console.WriteLine("Input not accepted, try again.");
            }
#endif
        }

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                string projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                    projectDisplay += $" ({loadProgress.TargetFramework})";

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}
