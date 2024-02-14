using System;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Core.Config
{
    public interface IConfigProvider
    {
        public ConfigRoot Config { get; }
    }
}
