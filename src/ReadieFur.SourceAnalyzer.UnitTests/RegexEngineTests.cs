using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    [CTest]
    public class RegexEngineTests
    {
        private const string PASCAL_CASE_SIMPLE = "([A-Z][a-z]+)+";

        #region Parse tests
        [MTest] public void ParseTest1() => new Core.RegexEngine.Regex(PASCAL_CASE_SIMPLE);
        #endregion

        #region Match tests
        [MTest] public void MatchTest1() => Assert.IsTrue(new Core.RegexEngine.Regex(PASCAL_CASE_SIMPLE).Test("PascalCase"));

        [MTest] public void UnmatchTest1() => Assert.IsFalse(new Core.RegexEngine.Regex(PASCAL_CASE_SIMPLE).Test("not_pascal_case"));
        #endregion

        #region Conform tests
        [MTest] public void ConformTest1() => Assert.AreEqual(new Core.RegexEngine.Regex(PASCAL_CASE_SIMPLE).Conform("PascalCase"), "PascalCase");
        
        [MTest] public void ConformTest2() => Assert.AreEqual(new Core.RegexEngine.Regex(PASCAL_CASE_SIMPLE).Conform("not_pascal_case"), "PascalCase");
        #endregion
    }
}
