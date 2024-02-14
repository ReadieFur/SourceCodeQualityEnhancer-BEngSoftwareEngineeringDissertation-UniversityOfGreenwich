//#define DO_SOLUTION_EVENT_CHECKS

using CSharpTools.Pipes;
using static CSharpTools.Pipes.Helpers;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace ReadieFur.SourceAnalyzer.VSIX.Configuration
{
    internal class HostConfigManager : AConfigManager
    {
        private PipeServerManager _pipeServerManager;
#if DO_SOLUTION_EVENT_CHECKS
        private string _currentSolution;
#endif

        public HostConfigManager(string ipcName) : base(ipcName)
        {
            _pipeServerManager = new PipeServerManager(ipcName, ComputeBufferSizeOf<SSharedConfigMessage>());
            _pipeServerManager.OnMessage += PipeServerManager_OnMessage;
        }

        private void PipeServerManager_OnMessage(Guid clientID, ReadOnlyMemory<byte> data)
        {
            SSharedConfigMessage message;
            try { message = Deserialize<SSharedConfigMessage>(data.ToArray()); }
            catch { return; }

            if (message.RequestConfiguration)
            {
                SSharedConfigMessage response = new()
                {
                    RequestConfiguration = true,
                    ConfigurationPath = GetConfigurationPath().ToCharArray()
                };
                var a = Serialize(response);
                var b = ComputeBufferSizeOf<SSharedConfigMessage>();
                _pipeServerManager.SendMessage(clientID, Serialize(response));
            }
        }

        public override void Dispose()
        {
            _pipeServerManager.Dispose();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
        public override ConfigRoot GetConfiguration()
        {
            lock (_lock)
            {
#if DO_SOLUTION_EVENT_CHECKS
                bool solutionChanged = false;
                ThreadHelper.JoinableTaskFactory.Run(delegate
                {
                    //The respective visual studio services SHOULD be ready at this point.
                    if (Package.GetGlobalService(typeof(DTE)) is not DTE dte)
                        throw new InvalidOperationException("Failed to get the DTE service.");

                    solutionChanged = _currentSolution != dte.Solution.FileName;
                    return Task.CompletedTask;
                });

                if (_cachedConfiguration is not null && !solutionChanged)
                    return _cachedConfiguration;
#else
                if (_cachedConfiguration is not null)
                    return _cachedConfiguration;
#endif

                //Load the configuration from the file path.
                _cachedConfiguration = YamlLoader.LoadAsync(GetConfigurationPath()).Result;

                if (_cachedConfiguration is null)
                    throw new InvalidOperationException("Failed to load the configuration.");

#if DO_SOLUTION_EVENT_CHECKS
                SSharedConfigMessage message = new()
                {
                    SolutionUpdated = true,
                    ConfigurationPath = GetConfigurationPath()
                };
                _pipeServerManager.BroadcastMessage(Serialize(message));
#endif

                return _cachedConfiguration;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread")]
        private static string GetConfigurationPath()
        {
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

            return configurationPath;
        }
    }
}
