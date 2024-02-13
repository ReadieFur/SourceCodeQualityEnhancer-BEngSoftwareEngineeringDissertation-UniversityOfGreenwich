using System;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#if NET472
using Microsoft.VisualStudio.Shell;
using EnvDTE;
#endif

namespace ReadieFur.SourceAnalyzer.Core.Config
{
    public static class ConfigLoader
    {
#if NET472
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
#if NET472
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                //A last resort temporary workaround for visual studio AsyncPackage failing to load in time, attempt to detect a workspace. UPDATE: Keeping as a backup for slow async loading of the VSIX VsPackage.
                DTE? dte = null;
                bool solutionChanged = false;
                ThreadHelper.JoinableTaskFactory.Run(delegate
                {
                    if (Package.GetGlobalService(typeof(DTE)) is not DTE _dte)
                        return Task.CompletedTask;
                    dte = _dte;

                    solutionChanged = currentSolution != dte.Solution.FileName;
                    return Task.CompletedTask;
                });

                if (_configuration is not null && !solutionChanged)
                    return _configuration;

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
                                break;
                            }
                        }
                        if (foundValidConfigurationFile)
                            break;
                    }
                }, Microsoft.VisualStudio.Threading.JoinableTaskCreationOptions.LongRunning);
#pragma warning restore VSTHRD010
#endif

                if (_configuration is null)
                    throw new NullReferenceException();
                return _configuration;
            }
            private set => _configuration = value;
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
