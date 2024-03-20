namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public sealed class ConfigRoot
    {
        public Naming Naming { get; set; } = new();
        public Formatting Formatting { get; set; } = new();
    }
}
