using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReadieFur.SourceAnalyzer.Core.Config
{
    public static class ConfigLoader
    {
        private const string RS1035_JUSTIFICATION = "Because I say so";

        private static IDeserializer _deserializer = ApplyCommonBuilderProperties(new DeserializerBuilder())
            .IgnoreUnmatchedProperties() //See: https://github.com/aaubry/YamlDotNet/issues/842
            .Build();

        private static ISerializer _serializer = ApplyCommonBuilderProperties(new SerializerBuilder())
            .Build();

        private static ConfigRoot? _configuration = null;

        public static bool FallbackToDisabledConfiguration = false;

        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = RS1035_JUSTIFICATION)]
        public static ConfigRoot Configuration
        {
            get
            {
                //If no configuration is loaded, attempt to find a configuration in the current directory (conveniently in Visual Studio the current directory is the active solution directory).
                //I am using this method as within this core library I do not want to depend on any Visual Studio IDE libraries as it should be redistributable.
                //I would like to, in the future if time permits for it, to revert this so that it always requires a configuration to be loaded first and that is to be done via the VSIX VsPackage.
                //Though as it is right now, I've spent too much time trying to work around this going as far as reverse engineerign the API (with little success).
                if (_configuration is null && !AutoLoadConfigFileAsync(Environment.CurrentDirectory).Result)
                {
                    if (FallbackToDisabledConfiguration)
                        _configuration = new();
                    else
                        throw new InvalidOperationException("A source-analyzer.yaml configuration file has not been loaded.");
                }

                return _configuration!;
            }
        }

        private static TBuilder ApplyCommonBuilderProperties<TBuilder>(TBuilder builder) where TBuilder : BuilderSkeleton<TBuilder>
        {
            builder.WithNamingConvention(UnderscoredNamingConvention.Instance);
            return builder;
        }

        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = RS1035_JUSTIFICATION)]
        private static async Task<bool> AutoLoadConfigFileAsync(string path, uint maxSearchDepth = 1, uint depth = 0)
        {
            //MaxSearchDepth set to default to x because solutions may typically be one folder deeper, e.g. solution.sln/project.csproj
            if (depth > maxSearchDepth)
                return false;

            foreach (string subpath in Directory.EnumerateFiles(path))
            {
                if (Path.GetFileName(subpath).EndsWith("source-analyzer.yaml") && await LoadAsync(subpath))
                    return true;
            }

            foreach (string subdir in Directory.EnumerateDirectories(path))
            {
                if (await AutoLoadConfigFileAsync(subdir, maxSearchDepth, depth + 1))
                    return true;
            }

            return false;
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
