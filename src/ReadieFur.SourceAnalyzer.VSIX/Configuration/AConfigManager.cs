using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;

namespace ReadieFur.SourceAnalyzer.VSIX.Configuration
{
    internal abstract class AConfigManager : IConfigManager, IDisposable
    {
        protected virtual object _lock { get; private set; } = new object();

        protected virtual ConfigRoot? _cachedConfiguration { get; set; } = null;

        public AConfigManager(string ipcName) {}

        public abstract void Dispose();

        public abstract ConfigRoot GetConfiguration();
    }
}
