using System;
using YamlDotNet.Serialization;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class AroundToken : PunctuationToken
    {
        [YamlMember(Alias = "left")]
        [Obsolete("This property is for deserialization only. Use Left instead.", false)]
        public bool? _left { get; set; }
#pragma warning disable CS0618 // Type or member is obsolete
        [YamlIgnore] public bool Left => _left ?? true;
#pragma warning restore CS0618

        [YamlMember(Alias = "right")]
        [Obsolete("This property is for deserialization only. Use Right instead.", false)]
        public bool? _right { get; set; }
#pragma warning disable CS0618 // Type or member is obsolete
        [YamlIgnore] public bool Right => _right ?? true;
#pragma warning restore CS0618
    }
}
