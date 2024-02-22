using ReadieFur.SourceAnalyzer.Core.RegexEngine;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using static ReadieFur.SourceAnalyzer.UnitTests.RegexEngine.Common;

namespace ReadieFur.SourceAnalyzer.UnitTests.RegexEngine
{
    public abstract class ConformTestsTemplate
    {
        public abstract string Pattern { get; }

        public abstract string ExpectedForPascalCaseInput { get; }

        public abstract string ExpectedForCamelCaseInput { get; }

        public abstract string ExpectedForSnakeCaseInput { get; }

        public abstract string ExpectedForLowercaseInput { get; }

        public abstract string ExpectedForUppercaseInput { get; }

        public abstract string ExpectedSnakeUppercaseInput { get; }

        public virtual SConformOptions ConformOptions => new()
        {
            GreedyQuantifierDelimiters = new[] { '_' },
            GreedyQuantifiersSplitOnCaseChange = true,
            InsertLiterals = true
        };

        public void ConformTest(string pattern, string input, SConformOptions conformOptions, string expected)
        {
            string actual = new Regex(pattern).Conform(input, conformOptions);
            Assert.AreEqual(expected, actual);
        }

        [MTest] public void FromPascalCase() => ConformTest(Pattern, INPUT_PASCAL_CASE, ConformOptions, ExpectedForPascalCaseInput);

        [MTest] public void FromCamelCase() => ConformTest(Pattern, INPUT_CAMEL_CASE, ConformOptions, ExpectedForCamelCaseInput);

        [MTest] public void FromSnakecase() => ConformTest(Pattern, INPUT_SNAKE_CASE, ConformOptions, ExpectedForSnakeCaseInput);

        [MTest] public void FromLowercase() => ConformTest(Pattern, INPUT_LOWERCASE, ConformOptions, ExpectedForLowercaseInput);

        [MTest] public void FromUppercase() => ConformTest(Pattern, INPUT_UPPERCASE, ConformOptions, ExpectedForUppercaseInput);

        [MTest] public void FromSnakeUppercase() => ConformTest(Pattern, INPUT_DOUBLE_UPPERCASE, ConformOptions, ExpectedSnakeUppercaseInput);
    }
}
