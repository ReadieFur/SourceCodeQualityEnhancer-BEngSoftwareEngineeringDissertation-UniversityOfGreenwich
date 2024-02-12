using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReadieFur.SourceAnalyzer.Core.Config
{
    public static class ConfigLoader
    {
        public static async Task<bool> Load(string path)
        {
            try
            {
                ConfigRoot config = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build()
                    .Deserialize<ConfigRoot>(path);
            }
            catch
            {
                return false;
            }

            return await Task.FromResult(true);
        }
    }
}
