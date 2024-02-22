using ReadieFur.SourceAnalyzer.Core.RegexEngine;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    [CTest]
    public class RegexEngineTests
    {
        #region Constants
        private const string PATTERN_PASCAL = "([A-Z][a-z]+)+";
        private const string PATTERN_CAMEL = "[a-z]+([A-Z][a-z]+)*";
        private const string PATTERN_SNAKE = "[a-z]+(_[a-z]+)*";

        private const string INPUT_PASCAL_CASE = "PascalCase";
        private const string INPUT_CAMEL_CASE = "camelCase";
        private const string INPUT_SNAKE_CASE = "snake_case";
        private const string INPUT_LOWERCASE = "lowercase";
        private const string INPUT_UPPERCASE = "UPPERCASE";
        private const string INPUT_DOUBLE_UPPERCASE = "DOUBLE_UPPERCASE";

        private static readonly SConformOptions CONFORM_OPTIONS = new()
        {
            GreedyQuantifierDelimiters = new[] { '_' },
            GreedyQuantifiersSplitOnCaseChange = true,
            InsertLiterals = true
        };
        #endregion

        #region Parse tests
        [MTest] public void ParsePascal() => new Regex(PATTERN_PASCAL);

        [MTest] public void ParseCamel() => new Regex(PATTERN_CAMEL);

        [MTest] public void ParseSnake() => new Regex(PATTERN_SNAKE);
        #endregion

        #region Match tests
        #region Pascal case
        [MTest] public void MatchPascal1() => Assert.IsTrue(new Regex(PATTERN_PASCAL).Test(INPUT_PASCAL_CASE));

        [MTest] public void MatchPascal2() => Assert.IsTrue(new Regex(PATTERN_PASCAL).Test("Pascal"));
        #endregion

        #region Camel case
        [MTest] public void MatchCamel1() => Assert.IsTrue(new Regex(PATTERN_CAMEL).Test(INPUT_CAMEL_CASE));

        [MTest] public void MatchCamel2() => Assert.IsTrue(new Regex(PATTERN_CAMEL).Test("camel"));
        #endregion

        #region Snake case
        [MTest] public void MatchSnake1() => Assert.IsTrue(new Regex(PATTERN_SNAKE).Test(INPUT_SNAKE_CASE));

        [MTest] public void MatchSnake2() => Assert.IsTrue(new Regex(PATTERN_SNAKE).Test("snake"));
        #endregion
        #endregion

        #region Conform tests
        private void ConformTest(string pattern, string input, string expected)
        {
            string actual = new Regex(pattern).Conform(input, CONFORM_OPTIONS);
            Assert.AreEqual(expected, actual);
        }

        #region Pascal case
        [MTest] public void PascalFromPascal() => ConformTest(PATTERN_PASCAL, INPUT_PASCAL_CASE, "PascalCase");

        [MTest] public void PascalFromCamel() => ConformTest(PATTERN_PASCAL, INPUT_CAMEL_CASE, "CamelCase");

        [MTest] public void PascalFromSnake() => ConformTest(PATTERN_PASCAL, INPUT_SNAKE_CASE, "SnakeCase");

        [MTest] public void PascalFromLower() => ConformTest(PATTERN_PASCAL, INPUT_LOWERCASE, "Lowercase");

        [MTest] public void PascalFromUpper() => ConformTest(PATTERN_PASCAL, INPUT_UPPERCASE, "Uppercase");

        [MTest] public void PascalFromDoubleUpper() => ConformTest(PATTERN_PASCAL, INPUT_DOUBLE_UPPERCASE, "DoubleUppercase");
        #endregion

        #region Camel case
        [MTest] public void CamelFromPascal() => ConformTest(PATTERN_CAMEL, INPUT_PASCAL_CASE, "pascalCase");

        [MTest] public void CamelFromCamel() => ConformTest(PATTERN_CAMEL, INPUT_CAMEL_CASE, "camelCase");

        [MTest] public void CamelFromSnake() => ConformTest(PATTERN_CAMEL, INPUT_SNAKE_CASE, "snakeCase");

        [MTest] public void CamelFromLower() => ConformTest(PATTERN_CAMEL, INPUT_LOWERCASE, "lowercase");

        [MTest] public void CamelFromUpper() => ConformTest(PATTERN_CAMEL, INPUT_UPPERCASE, "uppercase");

        [MTest] public void CamelFromDoubleUpper() => ConformTest(PATTERN_CAMEL, INPUT_DOUBLE_UPPERCASE, "doubleUppercase");
        #endregion

        #region Snake case
        [MTest] public void SnakeFromPascal() => ConformTest(PATTERN_SNAKE, INPUT_PASCAL_CASE, "pascal_case");

        [MTest] public void SnakeFromCamel() => ConformTest(PATTERN_SNAKE, INPUT_CAMEL_CASE, "camel_case");

        [MTest] public void SnakeFromSnake() => ConformTest(PATTERN_SNAKE, INPUT_SNAKE_CASE, "snake_case");

        [MTest] public void SnakeFromLower() => ConformTest(PATTERN_SNAKE, INPUT_LOWERCASE, "lowercase");

        [MTest] public void SnakeFromUpper() => ConformTest(PATTERN_SNAKE, INPUT_UPPERCASE, "uppercase");

        [MTest] public void SnakeFromDoubleUpper() => ConformTest(PATTERN_SNAKE, INPUT_DOUBLE_UPPERCASE, "double_uppercase");
        #endregion
        #endregion
    }
}
