using System;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.UnitTests.Compatibility
{
    public class CTestAttribute :
#if NETCOREAPP
        Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute
#elif NETSTANDARD
        NUnit.Framework.TestFixtureAttribute
#endif
    {
    }
}
