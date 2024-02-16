using ReadieFur.SourceAnalyzer.Core;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    [CTest]
    public class NamingEngineTests
    {
        private const string PASCAL_CASE = "^(?:[A-Z][a-z]+)*$";

        private static async Task RunTest(string input, string pattern)
        {
            await NamingEngine.ConformToPatternAsync(input, pattern);
        }

        [MTest] public async Task Test1() => await RunTest("_foo_bar", PASCAL_CASE);
    }
}
