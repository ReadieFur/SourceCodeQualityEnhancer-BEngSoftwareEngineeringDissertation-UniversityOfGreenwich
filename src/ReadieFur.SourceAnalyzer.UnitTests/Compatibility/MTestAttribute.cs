using System;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.UnitTests.Compatibility
{
    public class MTestAttribute :
#if NETCOREAPP || NETFRAMEWORK
        Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute
#elif NETSTANDARD
        NUnit.Framework.TestAttribute
#endif
    {
    }
}
