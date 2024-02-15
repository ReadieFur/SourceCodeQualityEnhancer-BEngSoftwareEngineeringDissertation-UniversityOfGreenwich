using System;
using System.Collections.Generic;
using System.Text;

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
