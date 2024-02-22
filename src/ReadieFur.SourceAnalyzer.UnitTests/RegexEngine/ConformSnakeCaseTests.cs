using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using static ReadieFur.SourceAnalyzer.UnitTests.RegexEngine.Common;

namespace ReadieFur.SourceAnalyzer.UnitTests.RegexEngine
{
    [CTest]
    public class ConformSnakeCaseTests : ConformTestsTemplate
    {
        public override string Pattern => PATTERN_SNAKE_CASE;

        public override string ExpectedForPascalCaseInput => "pascal_case";

        public override string ExpectedForCamelCaseInput => "camel_case";

        public override string ExpectedForSnakeCaseInput => "snake_case";

        public override string ExpectedForLowercaseInput => "lowercase";

        public override string ExpectedForUppercaseInput => "uppercase";

        public override string ExpectedSnakeUppercaseInput => "double_uppercase";
    }
}
