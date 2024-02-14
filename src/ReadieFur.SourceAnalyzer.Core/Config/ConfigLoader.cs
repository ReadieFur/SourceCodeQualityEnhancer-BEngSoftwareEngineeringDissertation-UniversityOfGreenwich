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

        public static ConfigRoot Configuration { get; private set; } = new();

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
                    Configuration = _deserializer.Deserialize<ConfigRoot>(await reader.ReadToEndAsync());
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
