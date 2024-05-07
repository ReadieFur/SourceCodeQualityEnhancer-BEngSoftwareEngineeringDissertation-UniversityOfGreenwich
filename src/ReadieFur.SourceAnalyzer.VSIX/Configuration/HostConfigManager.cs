using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace ReadieFur.SourceAnalyzer.VSIX.Configuration
{
    internal class HostConfigManager : AConfigManager
    {
        public HostConfigManager(string ipcName) : base(ipcName) {}

        //This method is always called before the client method, so we can safely assume that the configuration will be loaded before the client requests it.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread")]
        public override ConfigRoot GetConfiguration()
        {
            lock (_lock)
            {
                if (CachedConfiguration is not null)
                    return CachedConfiguration;

                string configurationPath = string.Empty;

                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    if (Package.GetGlobalService(typeof(DTE)) is not DTE dte)
                        throw new InvalidOperationException("Failed to get the DTE service.");

                    foreach (Project project in dte.Solution.Projects)
                    {
                        foreach (ProjectItem item in project.ProjectItems)
                        {
                            if (!item.FileNames[1].EndsWith("source-analyzer.yaml"))
                                continue;

                            if (await YamlLoader.LoadAsync(item.FileNames[1]) is not null)
                            {
                                configurationPath = item.FileNames[1];
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(configurationPath))
                            break;
                    }
                });

                if (string.IsNullOrEmpty(configurationPath))
                    throw new InvalidOperationException("Failed to find the configuration file.");

                //Load the configuration from the file path.
                CachedConfiguration = YamlLoader.LoadAsync(configurationPath).Result;

                if (CachedConfiguration is null)
                    throw new InvalidOperationException("Failed to load the configuration.");

                ConfigPath = configurationPath;
                WriteSharedMemory(configurationPath);

                return CachedConfiguration;
            }
        }
    }
}
