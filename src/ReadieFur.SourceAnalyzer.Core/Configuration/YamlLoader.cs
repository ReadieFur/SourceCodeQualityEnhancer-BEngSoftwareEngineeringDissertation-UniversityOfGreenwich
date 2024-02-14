using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public static class YamlLoader
    {
        private static IDeserializer _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties() //See: https://github.com/aaubry/YamlDotNet/issues/842
            .Build();

        public static async Task<ConfigRoot?> LoadAsync(string path)
        {
            try
            {
                //Instead of passing the StreamReader directly into the Deserialize method, we read it into a buffer as the YAML parser does not support async IO operations.
                using (StreamReader reader = new(path))
                    return _deserializer.Deserialize<ConfigRoot>(await reader.ReadToEndAsync());
            }
            catch
            {
                return null;
            }
        }
    }
}
