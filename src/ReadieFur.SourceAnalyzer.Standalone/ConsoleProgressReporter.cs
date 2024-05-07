using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
    {
        public static ConsoleProgressReporter Instance { get; private set; } = new();

        public void Report(ProjectLoadProgress loadProgress)
        {
            string projectDisplay = Path.GetFileName(loadProgress.FilePath);
            if (loadProgress.TargetFramework != null)
                projectDisplay += $" ({loadProgress.TargetFramework})";

            Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
        }
    }
}
