using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReadieFur.SourceAnalyzer.Core.RegexEngine;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using static ReadieFur.SourceAnalyzer.UnitTests.RegexEngine.Common;

namespace ReadieFur.SourceAnalyzer.UnitTests.RegexEngine
{
    [CTest]
    public class MatchTests
    {
        #region Pascal case
        [MTest] public void PascalDouble() => Assert.IsTrue(new Regex(PATTERN_PASCAL_CASE).Test(INPUT_PASCAL_CASE));

        [MTest] public void PascalSingle() => Assert.IsTrue(new Regex(PATTERN_PASCAL_CASE).Test("Pascal"));

        [MTest] public void PascalUnmatch() => Assert.IsFalse(new Regex(PATTERN_PASCAL_CASE).Test("not_pascal"));
        #endregion

        #region Camel case
        [MTest] public void CamelDouble() => Assert.IsTrue(new Regex(PATTERN_CAMEL_CASE).Test(INPUT_CAMEL_CASE));

        [MTest] public void CamelSingle() => Assert.IsTrue(new Regex(PATTERN_CAMEL_CASE).Test("camel"));

        [MTest] public void CamelUnmatch() => Assert.IsFalse(new Regex(PATTERN_CAMEL_CASE).Test("NotCamel"));
        #endregion

        #region Snake case
        [MTest] public void SnakeDouble() => Assert.IsTrue(new Regex(PATTERN_SNAKE_CASE).Test(INPUT_SNAKE_CASE));

        [MTest] public void SnakeSingle() => Assert.IsTrue(new Regex(PATTERN_SNAKE_CASE).Test("snake"));

        [MTest] public void SnakeUnmatch() => Assert.IsFalse(new Regex(PATTERN_SNAKE_CASE).Test("notSnake"));
        #endregion
    }
}
