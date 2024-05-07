using ReadieFur.SourceAnalyzer.Core.RegexEngine;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using static ReadieFur.SourceAnalyzer.UnitTests.RegexEngine.Common;

namespace ReadieFur.SourceAnalyzer.UnitTests.RegexEngine
{
    [CTest]
    public class ConformCamelCaseTests : ConformTestsTemplate
    {
        public override string Pattern => PATTERN_CAMEL_CASE;

        public override string ExpectedForPascalCaseInput => "pascalCase"; // "pascalCase

        public override string ExpectedForCamelCaseInput => "camelCase";

        public override string ExpectedForSnakeCaseInput => "snakeCase";

        public override string ExpectedForLowercaseInput => "lowercase";

        public override string ExpectedForUppercaseInput => "uppercase";

        public override string ExpectedSnakeUppercaseInput => "doubleUppercase";

        public override SConformOptions ConformOptions => CONFORM_OPTIONS;
    }
}
