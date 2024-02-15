using ReadieFur.SourceAnalyzer.VSIX.Configuration;
using ReadieFur.SourceAnalyzer.VSIX.Helpers;
using System;
using System.Diagnostics;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public partial class ConfigManager : IDisposable
    {
        /// <summary>
        /// Check if we are running under the visual studio environment or not.
        /// </summary>
        /// <returns></returns>
        internal static readonly bool IsVisualStudioInstance = Process.GetCurrentProcess().ProcessName == "devenv";

        internal AConfigManager Manager;

        public ConfigManager()
        {
            //Create a shared memory space for the configuration, this is required as it is possible for the analyzer and the extension to be running in separate processes.
            //We will use the devenv process ID as the key for the shared memory space.
            Process? process = Process.GetCurrentProcess();
            while (process is not null)
            {
                process = ParentProcessUtilities.GetParentProcess(process.Id);
                if (process?.ProcessName == "devenv")
                    break;
            }
            if (process is null)
                throw new InvalidOperationException("Failed to find the parent devenv process.");

            string ipcName = $"{AssemblyInfo.ASSEMBLY_NAME}_{nameof(ConfigManager)}";
            
            if (IsVisualStudioInstance)
                Manager = new HostConfigManager(ipcName);
            else
                Manager = new ClientConfigManager(ipcName);
        }

        ConfigRoot IConfigManager.GetConfiguration() => Manager.GetConfiguration();

        void IDisposable.Dispose() => Manager.Dispose();
    }
}
