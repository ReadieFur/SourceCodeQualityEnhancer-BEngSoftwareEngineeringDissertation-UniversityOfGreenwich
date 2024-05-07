using System;

namespace ReadieFur.SourceAnalyzer.UnitTests.TestFiles
{
    internal class Dummy
    {
        public void Foo()
        {
            int bar = 3;
        }
    }

//#0024
//-    ''class'' Punctuation
//+    internal class Punctuation
    {
//#0024
//-        ''int'' FooBar = 1;
//+        private int FooBar = 1;

//#0024
//-        ''void'' Baz() { }
//+        private void Baz() { }
    }
}
