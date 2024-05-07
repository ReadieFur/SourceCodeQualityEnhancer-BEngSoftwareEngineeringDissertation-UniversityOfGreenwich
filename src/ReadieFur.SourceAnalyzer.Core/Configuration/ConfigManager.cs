namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    //The move to a shared project based solution means that I can make the config loader a partial class that needs to be implemented by any other projects that use it.
    public partial class ConfigManager : IConfigManager
    {
        public static readonly ConfigManager Instance = new();

        public static ConfigRoot Configuration => ((IConfigManager)Instance).GetConfiguration();
    }
}
