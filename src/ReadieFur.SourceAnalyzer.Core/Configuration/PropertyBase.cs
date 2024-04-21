namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class ConfigBase
    {
        public const ESeverity DEFAULT_SEVERITY = ESeverity.Info;

        public ESeverity Severity { get; set; } = DEFAULT_SEVERITY;

        [YamlDotNet.Serialization.Callbacks.OnDeserialized]
        internal void OnDeserialized()
        {
            //TODO: Use a custom attribute to check if the required properties have been set.
        }
    }
}
