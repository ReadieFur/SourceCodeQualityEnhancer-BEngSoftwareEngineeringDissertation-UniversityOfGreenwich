using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class InferredBlock : ConfigBase
    {
        [YamlMember(Alias = "mode")]
        private string _mode { get; set; }

        [YamlIgnore]
        public bool IsInferred => _mode == "inferred";
    }
}
