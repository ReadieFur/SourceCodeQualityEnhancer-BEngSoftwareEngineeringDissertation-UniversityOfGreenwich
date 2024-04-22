using System;

namespace ReadieFur.SourceAnalyzer.UnitTests.TestFiles
{
    public class PunctuationNewLine
    {
        private static void Main(string[] args)
        {
//#0023
//-            if (true ''||'' true)
//+            if (true
//+                || true)
            {
            }
        }
    }
}
