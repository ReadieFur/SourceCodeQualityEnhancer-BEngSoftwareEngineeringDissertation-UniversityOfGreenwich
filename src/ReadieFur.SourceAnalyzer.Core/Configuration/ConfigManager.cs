namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public partial class ConfigManager : IConfigManager
    {
        private static readonly ConfigManager _instance = new();

        public static ConfigRoot Configuration => ((IConfigManager)_instance).GetConfiguration();
    }
}
