using ReadieFur.SourceAnalyzer.Core.RegexEngine;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using static ReadieFur.SourceAnalyzer.UnitTests.RegexEngine.Common;

namespace ReadieFur.SourceAnalyzer.UnitTests.RegexEngine
{
    [CTest]
    public class ConformPascalCaseTests : ConformTestsTemplate
    {
        public override string Pattern => PATTERN_PASCAL_CASE;

        public override string ExpectedForPascalCaseInput => "PascalCase";

        public override string ExpectedForCamelCaseInput => "CamelCase";

        public override string ExpectedForSnakeCaseInput => "SnakeCase";

        public override string ExpectedForLowercaseInput => "Lowercase";

        public override string ExpectedForUppercaseInput => "Uppercase";

        public override string ExpectedSnakeUppercaseInput => "DoubleUppercase";

        public override SConformOptions ConformOptions => CONFORM_OPTIONS;
    }
}
