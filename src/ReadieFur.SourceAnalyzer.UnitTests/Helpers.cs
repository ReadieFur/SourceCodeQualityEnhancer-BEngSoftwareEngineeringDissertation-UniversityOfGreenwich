using ReadieFur.SourceAnalyzer.Core.Configuration;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    internal static class Helpers
    {
        public static async Task LoadConfiguration()
        {
            string? path = Directory.EnumerateFiles(Environment.CurrentDirectory).FirstOrDefault(path => path.EndsWith("source-analyzer.yaml"));
            Assert.False(string.IsNullOrEmpty(path), "Could not find source-analyzer.yaml");
            Assert.True(await ConfigManager.Instance.LoadAsync(path!), "Failed to load source-analyzer.yaml");
        }
    }
}
