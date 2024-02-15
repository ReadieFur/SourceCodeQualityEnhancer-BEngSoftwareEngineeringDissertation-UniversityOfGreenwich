namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public partial class ConfigManager
    {
        private ConfigRoot? Config { get; set; }

        ConfigRoot IConfigManager.GetConfiguration()
        {
            if (Config is null)
                throw new InvalidOperationException("Configuration has not been loaded");
            return Config;
        }

        public async Task<bool> LoadAsync(string configPath)
        {
            Config = await YamlLoader.LoadAsync(configPath);
            return Config is not null;
        }
    }
}
