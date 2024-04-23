using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class InferredBlock : ConfigBase
    {
        [YamlMember(Alias = "mode")]
        [Obsolete("This property is for deserialization only. Use IsInferred instead.", false)]
        public string _mode { get; set; }

        [YamlIgnore]
#pragma warning disable CS0618 // Type or member is obsolete
        public bool IsInferred => _mode == "implicit";
#pragma warning restore CS0618
    }
}
