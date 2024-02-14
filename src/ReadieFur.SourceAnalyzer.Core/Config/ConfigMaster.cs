using System;
using System.Reflection;

namespace ReadieFur.SourceAnalyzer.Core.Config
{
    //Janky dynamic config loader that only exists for VSIX compatibility without being directly tied to any Visual Studio specific code.
    //The downside to this is it adds complexity and more importantly introduces runtime errors that would otherwise be caught at compile time.
    public class ConfigMaster : IConfigProvider
    {
        #region Static
        private static ConfigMaster _instance = new();

        private static readonly object _lock = new();

        public static IConfigProvider? Provider
        {
            get
            {
                lock (_lock)
                {
                    return _instance._provider;
                }
            }
            set
            {
                lock (_lock)
                {
                    _instance._provider = value;
                }
            }
        }

        public static ConfigRoot Configuration => _instance.Config;
        #endregion

        #region Instance
        private IConfigProvider? _provider = null;

        public ConfigRoot Config
        {
            get
            {
                lock (_lock)
                {
                    if (_provider is null && !FindProvider())
                        throw new InvalidOperationException("No configuration provider found.");
                    return _provider!.Config;
                }
            }
        }

        private bool FindProvider()
        {
            //https://stackoverflow.com/questions/4692340/find-types-in-all-assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    //Find the first matching valid implementation of IConfigProvider use that as the provider.
                    if (/*Look for ConfigMasterAttributes*/ type.GetCustomAttribute<ConfigProviderAttribute>() is not null
                        && /*Ensure the type has the IConfigMaster interface*/ type.GetInterface(nameof(IConfigProvider)) is not null
                        && /*Make sure that the type has a parameterless constructor*/ type.GetConstructor(Type.EmptyTypes) is not null)
                    {
                        _provider = (IConfigProvider)Activator.CreateInstance(type);
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion
    }
}
