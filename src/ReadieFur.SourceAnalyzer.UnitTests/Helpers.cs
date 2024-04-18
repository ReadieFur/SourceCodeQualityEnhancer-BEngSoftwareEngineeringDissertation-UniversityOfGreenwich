using ReadieFur.SourceAnalyzer.Core.Configuration;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    internal static class Helpers
    {
        public static async Task LoadConfiguration()
        {
            string? path = Directory.EnumerateFiles(Environment.CurrentDirectory).FirstOrDefault(path => path.EndsWith("source-analyzer.yaml"));
            Assert.IsFalse(string.IsNullOrEmpty(path), "Could not find source-analyzer.yaml");
            Assert.IsTrue(await ConfigManager.Instance.LoadAsync(path!), "Failed to load source-analyzer.yaml");
        }

        public static void SetConfiguration(ConfigRoot config)
        {
            (typeof(ConfigManager).GetProperty("Config") ?? throw new NullReferenceException()).SetValue(ConfigManager.Instance, config);
        }

        public static async Task<string> GetSourceFile(string fileName)
        {
            if (GetSolutionPath() is not string solutionPath)
            {
                Assert.Fail("Could not find solution directory.");
                return string.Empty;
            }

            if (FindFile(solutionPath, fileName) is not string sourceFile)
            {
                Assert.Fail($"Could not find source file: {fileName}");
                return string.Empty;
            }

            return await File.ReadAllTextAsync(sourceFile);
        }

        private static string? GetSolutionPath()
        {
            string? directory = Environment.CurrentDirectory;
            do
            {
                if (Directory.EnumerateFiles(directory).Any(file => file.EndsWith(".sln")))
                    return directory;
                directory = Directory.GetParent(directory)?.FullName;
            }
            while (directory != null);

            //Assert.Fail("Could not find solution file.");
            return null;
        }

        private static string? FindFile(string directory, string fileName)
        {
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                if (Path.GetFileName(file)!.ToLower() == fileName.ToLower())
                    return file;
            }

            foreach (string subDirectory in Directory.EnumerateDirectories(directory))
            {
                string directoryName = Path.GetFileName(subDirectory)!;
                if (new[] { "bin", "obj", "." }.Any(directoryName.StartsWith))
                    continue;

                if (FindFile(subDirectory, fileName) is string file)
                    return file;
            }

            return null;
        }
    }
}
