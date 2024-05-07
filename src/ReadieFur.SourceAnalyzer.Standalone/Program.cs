using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal class Program
    {
        private static MSBuildWorkspace? _workspace = null;
        private static Analyzer? _analyzer = null;
        private static FileDiffWindow? _fileDiffWindow = null;

        internal static async Task Main(string[] args)
        {
#if DEBUG && false
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                Thread dbgThread = new(_ =>
                {
                    FileDiffWindow fileDiffWindow = new();
                    fileDiffWindow.ShowDialog();
                });
                dbgThread.SetApartmentState(ApartmentState.STA);
                //Objects to be shared across threads must be passed here.
                dbgThread.Start();
                dbgThread.Join();
            }
            else
            {
                FileDiffWindow mainWindow = new();
                mainWindow.ShowDialog();
            }
            return;
#endif

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

            _workspace = MSBuildWorkspace.Create();

            try
            {
                //Print message for WorkspaceFailed event to help diagnosing project load failures.
                _workspace.WorkspaceFailed += (_, e) =>
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
#if RELEASE || (DEBUG && false)
                string path =
                    args.Length > 0 && (args[0].EndsWith(".sln") || args[0].EndsWith(".csproj")) && File.Exists(args[0])
                    ? args[0]
                    : Helpers.GetSolution();
#else
                string path = Helpers.GetSolution();
#endif
                Console.WriteLine($"Loading solution '{path}'");

                //Attach progress reporter so we print projects as they are loaded.
                Solution solution;
                if (path.EndsWith(".sln"))
                {
                    solution = await _workspace.OpenSolutionAsync(path, ConsoleProgressReporter.Instance);
                    Console.WriteLine($"Finished loading solution '{path}'");
                }
                else
                {
                    solution = (await _workspace.OpenProjectAsync(path, ConsoleProgressReporter.Instance)).Solution;
                    Console.WriteLine($"Finished loading project '{path}'");
                }

                //Load an analyzer configuration file.
                await Helpers.LoadAnalyzerConfigurationAsync(solution, args);

                if (!solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
                    throw new Exception();

                //Perform analysis on the projects in the loaded solution.
                _analyzer = await Analyzer.AnalyzeSolution(solution);

                //Display the results in a new window.
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    //Spawn a new STA thread to display the window if the main process thread is not STA as UI components require to be run on an STA thread.
                    Thread thread = new(solutionChangesRef =>
                    {
                        _fileDiffWindow = new((SolutionChanges)solutionChangesRef);
                        _fileDiffWindow.OnSaveAsNew += FileDiffWindow_OnSaveAsNew;
                        _fileDiffWindow.OnSaveInPlace += FileDiffWindow_OnSaveInPlace;
                        _fileDiffWindow.ShowDialog();
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    //Objects to be shared across threads must be passed here.
                    thread.Start(_analyzer.SolutionChanges);
                    thread.Join();
                }
                else
                {
                    _fileDiffWindow = new(_analyzer.SolutionChanges);
                    _fileDiffWindow.OnSaveAsNew += FileDiffWindow_OnSaveAsNew;
                    _fileDiffWindow.OnSaveInPlace += FileDiffWindow_OnSaveInPlace;
                    _fileDiffWindow.ShowDialog();
                }
            }
            catch {}
            finally
            {
                _workspace.Dispose();

                //Unregister the instance of MSBuild.
                MSBuildLocator.Unregister();
            }
        }

        private static void FileDiffWindow_OnSaveInPlace()
        {
            if (_workspace is null || _analyzer is null)
            {
                Console.WriteLine("ERROR: Workspace is null.");
                return;
            }

            //Save the changes to the files in the workspace.
            if (!_workspace.TryApplyChanges(_analyzer.NewSolution))
            {
                Console.WriteLine("ERROR: Failed to apply changes to the workspace.");
            }
            else
            {
                Console.WriteLine("INFO: Changes applied to the workspace.");
                System.Windows.Forms.MessageBox.Show("The changes have been saved to the disk.", "Success", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                _fileDiffWindow?.Close();
            }
        }

        private static void FileDiffWindow_OnSaveAsNew(string targetDirectory)
        {
            if (_workspace is null || _analyzer is null)
            {
                Console.WriteLine("ERROR: Workspace is null.");
                return;
            }

            //Save the new workspace to a new directory.
            throw new NotImplementedException();

            _fileDiffWindow?.Close();
        }
    }
}
