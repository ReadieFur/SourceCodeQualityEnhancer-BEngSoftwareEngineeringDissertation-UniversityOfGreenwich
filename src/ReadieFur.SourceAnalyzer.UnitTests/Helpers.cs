using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Reflection;
using System.Text;

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
            (typeof(ConfigManager).GetProperty("Config", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NullReferenceException()).SetValue(ConfigManager.Instance, config);
        }

        public static void OverrideConfiguration(ConfigRoot config)
        {
            throw new NotImplementedException();
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

            //https://stackoverflow.com/questions/59700704/proper-way-to-read-files-asynchronously
#if !NETFRAMEWORK
            return await File.ReadAllTextAsync(sourceFile);
#elif false
            using (FileStream stream = new(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                StringBuilder stringBuilder = new();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.Unicode.GetString(buffer, 0, numRead);
                    stringBuilder.Append(text);
                }
                return stringBuilder.ToString();
            }
#else
            return File.ReadAllText(sourceFile);
#endif
        }

        public static string? GetSolutionPath()
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

        public static string? FindFile(string directory, string fileName)
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
