namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    //The move to a shared project based solution means that I can make the config loader a partial class that needs to be implemented by any other projects that use it.
    public partial class ConfigManager : IConfigManager
    {
        private static readonly ConfigManager _instance = new();

        public static ConfigRoot Configuration => ((IConfigManager)_instance).GetConfiguration();
    }
}
