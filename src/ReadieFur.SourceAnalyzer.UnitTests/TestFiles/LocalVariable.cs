using System;

namespace ReadieFur.SourceAnalyzer.UnitTests.TestFiles
{
    public class LocalVariable
    {
        public void Foo(string[] args)
        {
            //My analyzer cannot do multiple checks within the same file in its current state.
            //string /0011/FOOBAR/foobar/ = "foobar";
            string /*0011*/FOO_BAR/*fooBar*/ = "foobar";
        }
    }
}
