using System;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.UnitTests.TestFiles
{
    internal class Comment
    {
//#0020
//-        public int foo = 0; ''//My comment.''
//+        //My comment.
//+        public int foo = 0;
    }
}
