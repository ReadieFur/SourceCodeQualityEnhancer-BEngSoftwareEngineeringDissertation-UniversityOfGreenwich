using System;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.UnitTests.TestFiles
{
//For this first block below I have had to wrap the "internal" word in double-single quotes as a workaround for my file parser
//as the diagnostic analyzer will look for the first non-whitespace token in the document (which in this case is the "internal" part of the string).
//#0017
//-''internal'' class Indentation
//+    internal class Indentation
//#0017
//-{
//+    {
//#0017
//-}
//+    }
}
