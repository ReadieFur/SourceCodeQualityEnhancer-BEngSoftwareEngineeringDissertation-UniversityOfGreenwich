﻿using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    [CTest]
    public class RegexEngineTests
    {
        private const string PASCAL_CASE_SIMPLE = "([A-Z][a-z]+)+";

        [MTest] public void Test1() => new Core.RegexEngine.Regex(PASCAL_CASE_SIMPLE);

        [MTest] public void Test2() => new Core.RegexEngine.Regex(PASCAL_CASE_SIMPLE).Test("PascalCase");
    }
}
