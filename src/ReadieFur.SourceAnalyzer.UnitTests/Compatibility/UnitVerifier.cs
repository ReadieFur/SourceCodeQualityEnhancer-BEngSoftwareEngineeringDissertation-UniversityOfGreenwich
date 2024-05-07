#if false
namespace ReadieFur.SourceAnalyzer.UnitTests.Compatibility
{
    public class UnitVerifier :
#if NETCOREAPP
        Microsoft.CodeAnalysis.Testing.Verifiers.MSTestVerifier
#elif NETSTANDARD
        Microsoft.CodeAnalysis.Testing.Verifiers.NUnitVerifier
#endif
    {
    }
}
#endif
