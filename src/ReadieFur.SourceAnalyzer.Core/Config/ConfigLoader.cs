using System;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Threading;
using System.Text;

#if VSIX
using Process = System.Diagnostics.Process;
using System.IO.MemoryMappedFiles;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
#endif

namespace ReadieFur.SourceAnalyzer.Core.Config
{
    public static class ConfigLoader
    {
#if VSIX
        //Roslyn has issues loading external dlls referenced by this libray. It attempts to look in "$(IDE)\CommonExtensions\Microsoft\VBCSharp\LanguageServices\Core" when it should ook in the directory of this assembly.
        //These problems exist as I am doing things that the Roslyn api was not intended for/is not reccomended for (dynamic analyzer configurations).
        //See: https://stackoverflow.com/questions/70353616/c-sharp-roslyn-analyzer-via-projectreference-leads-to-not-found-error-after-open
        private static Mutex mutex;
        private static MemoryMappedFile mappedFile;
        private static MemoryMappedViewAccessor accessor;
        private static string? currentSolution = null;
#endif

        private static IDeserializer _deserializer = ApplyCommonBuilderProperties(new DeserializerBuilder())
            .IgnoreUnmatchedProperties() //See: https://github.com/aaubry/YamlDotNet/issues/842
            .Build();

        private static ISerializer _serializer = ApplyCommonBuilderProperties(new SerializerBuilder())
            .Build();

        private static ConfigRoot? _configuration = null;

        public static ConfigRoot Configuration
        {
            get
            {
#if VSIX
                //Hacky VSIX/Roslyn compatibility stuffs.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                #region DTE pre-checks
                DTE? dte = null;
                bool solutionChanged = false;
                try
                {
                    ThreadHelper.JoinableTaskFactory.Run(delegate
                    {
                        if (Package.GetGlobalService(typeof(DTE)) is not DTE _dte)
                            return Task.CompletedTask;
                        dte = _dte;

                        solutionChanged = currentSolution != dte.Solution.FileName;
                        return Task.CompletedTask;
                    });
                }
                catch
                {
                    //This can fail when running from within the Roslyn instance under devenv.exe so fall just continue to the next section.
                }
                #endregion

                if (_configuration is not null && !solutionChanged)
                    return _configuration;

                #region Load from shared memory
                //This current solution has issues of it's own as it cannot detect solution changes, but I believe the instance will be reloaded if a new solution is opened? Besides this method should only ever be called once per instance iirc.

                mutex.WaitOne();
                string? configFile;
                try
                {
                    ushort size = accessor.ReadUInt16(54);
                    byte[] buffer = new byte[size];
                    accessor.ReadArray(54 + 2, buffer, 0, buffer.Length);
                    configFile = Encoding.ASCII.GetString(buffer);
                }
                catch { configFile = null; }
                mutex.ReleaseMutex();

                if (configFile is not null && !string.IsNullOrEmpty(configFile) && LoadAsync(configFile).Result && _configuration is not null)
                    return _configuration;
                #endregion

                #region Load from DTE
                //A last resort temporary workaround for Visual Studio AsyncPackage failing to load in time, attempt to detect a workspace. UPDATE: Keeping as a backup for slow async loading of the VSIX VsPackage.

                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    if (dte is null)
                        throw new NullReferenceException();

                    currentSolution = dte.Solution.FileName;

                    foreach (Project project in dte.Solution.Projects)
                    {
                        bool foundValidConfigurationFile = false;
                        foreach (ProjectItem item in project.ProjectItems)
                        {
                            if (!item.FileNames[1].EndsWith("source-analyzer.yaml"))
                                continue;

                            if (await LoadAsync(item.FileNames[1]))
                            {
                                foundValidConfigurationFile = true;

                                byte[] buffer = Encoding.ASCII.GetBytes(item.FileNames[1]);
                                accessor.Write(54, (ushort)buffer.Length);
                                accessor.WriteArray(54 + 2, buffer, 0, buffer.Length);

                                break;
                            }
                        }
                        if (foundValidConfigurationFile)
                            break;
                    }
                }, Microsoft.VisualStudio.Threading.JoinableTaskCreationOptions.LongRunning);
                #endregion
#pragma warning restore VSTHRD010
#endif

                if (_configuration is null)
                    throw new NullReferenceException();
                return _configuration;
            }
            private set => _configuration = value;
        }

        static ConfigLoader()
        {
#if VSIX
            //Through debugging it seems that the VisualStudio and Roslyn tasks are spawned under different processes.
            //This causes problems as I need to dynamically load a configuration file which Roslyn cannot find due to it seemingly not having access to the VisualStudio shell API.
            //My workaround for this is to create a shared memory space for the two processes to communicate. Conveniently the VisualStudio instance will ALWAYS load the file first so we can safley read to the shared space.
            //This issue seems to only exist for the VSIX build of this project hence this is only targeting the VSIX build.

            //Generate an IPC name based on the parent devenv.exe process ID.
            Process? process = Process.GetCurrentProcess();
            while (process is not null && process.ProcessName != "devenv")
                process = VSIXHelpers.ParentProcessUtilities.GetParentProcess(process.Id);
            if (process is null)
                throw new Exception("Target parent process not found.");

            string ipcName = $"{nameof(ReadieFur)}_{nameof(SourceAnalyzer)}_{process.Id}";

            //From my source: https://github.com/ReadieFur/CSharpTools/blob/main/src/CSharpTools.SharedMemory/SharedMemory.cs
            mutex = new Mutex(false, ipcName + "_mutex");
            mutex.WaitOne();

            string mapName = ipcName + "_" + nameof(ConfigLoader);
            try { mappedFile = MemoryMappedFile.OpenExisting(mapName); }
            catch (FileNotFoundException)
            {
                //https://stackoverflow.com/questions/10806518/write-string-data-to-memorymappedfile
                //54 ASCII offset?, 2 size offset?, 260 Windows max file path, 1 null character?.
                mappedFile = MemoryMappedFile.CreateNew(mapName, 54 + 2 + 260 + 1);
            } 

            accessor = mappedFile.CreateViewAccessor();

            mutex.ReleaseMutex();
#endif
        }

        private static TBuilder ApplyCommonBuilderProperties<TBuilder>(TBuilder builder) where TBuilder : BuilderSkeleton<TBuilder>
        {
            builder.WithNamingConvention(UnderscoredNamingConvention.Instance);
            return builder;
        }

        public static async Task<bool> LoadAsync(string path)
        {
            try
            {
                //Instead of passing the StreamReader directly into the Deserialize method, we read it into a buffer as the YAML parser does not support async IO operations.
                using (StreamReader reader = new(path))
                {
                    _configuration = _deserializer.Deserialize<ConfigRoot>(await reader.ReadToEndAsync());
                    //object? v = _deserializer.Deserialize(await reader.ReadToEndAsync());
                }
            }
            catch
            {
                return false;
            }

            return await Task.FromResult(true);
        }
    }
}
