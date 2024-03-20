using YamlDotNet.Serialization;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class ConfigBase
    {
        public bool? Enabled { get; set; } = true;
        public ESeverity Severity { get; set; } = ESeverity.Info;

        //When Enabled is null, assume the property is set (in place so that the user does not have to explicitly set the property to true).
        [YamlIgnore] public bool IsEnabled => Enabled ?? true;
    }
}
