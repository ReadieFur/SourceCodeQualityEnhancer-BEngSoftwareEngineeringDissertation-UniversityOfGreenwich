using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal class Program
    {
        [STAThread]
        internal static async Task Main(string[] args)
        {
            //Attempt to set the version of MSBuild.
            VisualStudioInstance[] visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            VisualStudioInstance instance = visualStudioInstances.Length == 1
                //If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                //Handle selecting the version of MSBuild you want to use.
                : Helpers.SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            /* NOTE: Be sure to register an instance with the MSBuildLocator 
             * before calling MSBuildWorkspace.Create()
             * otherwise, MSBuildWorkspace won't MEF compose.
            */
            MSBuildLocator.RegisterInstance(instance);

            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                //Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (_, e) =>
                {
                    //Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: " + e.Diagnostic.Message);
                    //Console.ForegroundColor = ConsoleColor.Gray;
                    //Environment.Exit(1);
                };
                /*workspace.WorkspaceChanged += (_, e) =>
                {
                    if (e.DocumentId is null)
                        return;
                    Console.WriteLine($"INFO: Document '{e.DocumentId}' changed.");
                };*/

                //Get a solution to load from the user.
                string path =
                    args.Length > 0 && (args[0].EndsWith(".sln") || args[0].EndsWith(".csproj")) && File.Exists(args[0])
                    ? args[0]
                    : Helpers.GetSolution();
                Console.WriteLine($"Loading solution '{path}'");

                //Attach progress reporter so we print projects as they are loaded.
                Solution solution;
                if (path.EndsWith(".sln"))
                {
                    solution = await workspace.OpenSolutionAsync(path, ConsoleProgressReporter.Instance);
                    Console.WriteLine($"Finished loading solution '{path}'");
                }
                else
                {
                    solution = (await workspace.OpenProjectAsync(path, ConsoleProgressReporter.Instance)).Solution;
                    Console.WriteLine($"Finished loading project '{path}'");
                }

                //Load an analyzer configuration file.
                await Helpers.LoadAnalyzerConfigurationAsync(solution, args);

                if (!solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
                    throw new Exception();

                //Perform analysis on the projects in the loaded solution.
                SolutionChanges solutionChanges = await Analyzer.AnalyzeSolution(solution);
            }
        }
    }
}
