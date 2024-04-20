using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal static class Helpers
    {
        public static async Task LoadAnalyzerConfigurationAsync(Solution solution, string[]? args = null)
        {
            if (args is null)
                args = [];

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

        public static string GetSolution()
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

        public static bool FindSolutionFile(string path, out string? solutionFile)
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

        public static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"\tName: {visualStudioInstances[i].Name}");
                Console.WriteLine($"\tVersion: {visualStudioInstances[i].Version}");
                Console.WriteLine($"\tMSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

#if DEBUG || (DEBUG && true)
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
    }
}
