using System;

namespace ReadieFur.SourceAnalyzer.UnitTests.TestFiles
{
    internal class ObjectStructure_PropertiesAtTop
    {
//#0026
//-        private void Foo() {}
//-        ''public int Bar { get; set; }''
//+        public int Bar { get; set; }
//+        private void Foo() {}
    }
}
