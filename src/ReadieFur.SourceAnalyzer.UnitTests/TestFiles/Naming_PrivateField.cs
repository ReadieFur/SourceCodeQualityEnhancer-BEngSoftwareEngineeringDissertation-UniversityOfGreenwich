using System;

namespace ReadieFur.SourceAnalyzer.UnitTests.TestFiles
{
    public class PrivateField
    {
//#0001
//-        private string ''__foo_bar'' = "foobar";
//+        private string _fooBar = "foobar";
    }
}
