using YamlDotNet.Serialization;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class SpaceAround : PunctuationToken
    {
        [YamlMember(Alias = "left")] public bool? _left { get; set; }
        [YamlIgnore] public bool Left => _left ?? true;

        [YamlMember(Alias = "right")] public bool? _right { get; set; }
        [YamlIgnore] public bool Right => _right ?? true;
    }
}
