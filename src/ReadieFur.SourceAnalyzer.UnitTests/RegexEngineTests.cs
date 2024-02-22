using ReadieFur.SourceAnalyzer.Core.RegexEngine;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    [CTest]
    public class RegexEngineTests
    {
        private const string PASCAL_CASE_SIMPLE = "([A-Z][a-z]+)+";
        private static readonly SConformOptions CONFORM_OPTIONS = new()
        {
            GreedyQuantifierDelimiters = new[] { '_' },
            GreedyQuantifiersSplitOnCaseChange = true
        };

        #region Parse tests
        [MTest] public void ParseTest1() => new Regex(PASCAL_CASE_SIMPLE);
        #endregion

        #region Match tests
        [MTest] public void MatchTest1() => Assert.IsTrue(new Regex(PASCAL_CASE_SIMPLE).Test("PascalCase"));

        [MTest] public void MatchTest2() => Assert.IsTrue(new Regex(PASCAL_CASE_SIMPLE).Test("Pascal"));

        [MTest] public void UnmatchTest1() => Assert.IsFalse(new Regex(PASCAL_CASE_SIMPLE).Test("not_pascal_case"));
        #endregion

        #region Conform tests
        [MTest] public void ConformTest1() => Assert.AreEqual("PascalCase", new Regex(PASCAL_CASE_SIMPLE).Conform("PascalCase", CONFORM_OPTIONS));

        [MTest] public void ConformTest2() => Assert.AreEqual("Pascal", new Regex(PASCAL_CASE_SIMPLE).Conform("Pascal", CONFORM_OPTIONS));
        
        [MTest] public void ConformTest3() => Assert.AreEqual("NotPascalCase", new Regex(PASCAL_CASE_SIMPLE).Conform("not_pascal_case", CONFORM_OPTIONS));

        [MTest] public void ConformTest4() => Assert.AreEqual("Lowercase", new Regex(PASCAL_CASE_SIMPLE).Conform("lowercase", CONFORM_OPTIONS));

        [MTest] public void ConformTest5() => Assert.AreEqual("Uppercase", new Regex(PASCAL_CASE_SIMPLE).Conform("UPPERCASE", CONFORM_OPTIONS));

        [MTest] public void ConformTest6() => Assert.AreEqual("DoubleUppercase", new Regex(PASCAL_CASE_SIMPLE).Conform("DOUBLE_UPPERCASE", CONFORM_OPTIONS));
        #endregion
    }
}
