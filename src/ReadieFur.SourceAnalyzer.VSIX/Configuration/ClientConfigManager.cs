using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;

namespace ReadieFur.SourceAnalyzer.VSIX.Configuration
{
    internal class ClientConfigManager : AConfigManager
    {
        //My library I believe checks for race conditions and waits for a pipe to exist, however we don't need to check regardless as the host pipe will always be created first by the VSIX extension.
        public ClientConfigManager(string ipcName) : base(ipcName) { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
        public override ConfigRoot GetConfiguration()
        {
            lock (_lock)
            {
                if (CachedConfiguration is not null)
                    return CachedConfiguration;

                //Wait for the configuration to be received.
                string path = ReadSharedMemory(
#if !DEBUG
                    5000 //Allow up to x milliseconds for the configuration to be received, it it takes any longer, assume it failed.
#endif
                );

                if (string.IsNullOrEmpty(path))
                    throw new InvalidOperationException("Failed to receive the configuration from the host.");
                
                CachedConfiguration = YamlLoader.LoadAsync(path).Result;
                if (CachedConfiguration is null)
                    throw new InvalidOperationException("Failed to load the configuration from the host.");

                ConfigPath = path;

                return CachedConfiguration;
            }
        }
    }
}
