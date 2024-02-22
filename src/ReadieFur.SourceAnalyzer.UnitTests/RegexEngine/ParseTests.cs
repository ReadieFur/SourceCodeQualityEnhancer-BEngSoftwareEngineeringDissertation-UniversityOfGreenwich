using ReadieFur.SourceAnalyzer.Core.RegexEngine;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using static ReadieFur.SourceAnalyzer.UnitTests.RegexEngine.Common;

namespace ReadieFur.SourceAnalyzer.UnitTests.RegexEngine
{
    [CTest]
    public class ParseTests
    {
        [MTest] public void Pascal() => new Regex(PATTERN_PASCAL_CASE);

        [MTest] public void Camel() => new Regex(PATTERN_CAMEL_CASE);

        [MTest] public void Snake() => new Regex(PATTERN_SNAKE_CASE);
    }
}
