namespace ReadieFur.SourceAnalyzer.Core.Config
{
    public abstract class ConfigBase
    {
        public bool Enabled { get; set; } = false;
        public ESeverity Severity { get; set; }
    }
}
