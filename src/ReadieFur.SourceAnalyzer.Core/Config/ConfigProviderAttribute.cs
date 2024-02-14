using System;

namespace ReadieFur.SourceAnalyzer.Core.Config
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ConfigProviderAttribute : Attribute
    {
        public ConfigProviderAttribute() { }
    }
}
