using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class NamingTests : AAnalyzerTester
    {
        [MTest] public async Task ClassNameAnalyzer() => await TestAnalyzer<NamingAnalyzer>("ClassName.cs");

        [MTest] public async Task ClassNameFixProvider() => await TestFixProvider<NamingAnalyzer, NamingFixProvider>("ClassName.cs");

        //[MTest] public async Task LocalVariableFixProvider() => await TestFixProvider<NamingAnalyzer, NamingFixProvider>(typeof(LocalVariable));
    }
}
