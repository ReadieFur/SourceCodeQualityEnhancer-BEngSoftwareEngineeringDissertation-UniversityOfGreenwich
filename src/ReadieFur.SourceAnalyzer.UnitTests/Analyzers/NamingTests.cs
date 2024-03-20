using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using ReadieFur.SourceAnalyzer.UnitTests.TestFiles;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class NamingTests : AAnalyzerTester
    {
        [MTest] public async Task ClassNameAnalyzer() => await TestAnalyzer<NamingAnalyzer>(typeof(_class_name_));

        [MTest] public async Task ClassNameFixProvider() => await TestFixProvider<NamingAnalyzer, NamingFixProvider>(typeof(_class_name_));

        [MTest] public async Task LocalVariableFixProvider() => await TestFixProvider<NamingAnalyzer, NamingFixProvider>(typeof(LocalVariable));
    }
}
