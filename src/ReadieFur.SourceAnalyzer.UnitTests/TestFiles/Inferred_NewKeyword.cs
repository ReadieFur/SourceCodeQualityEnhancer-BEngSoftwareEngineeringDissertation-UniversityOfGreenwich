using System;

namespace ReadieFur.SourceAnalyzer.UnitTests.TestFiles
{
    internal class Dummy
    {
        public int Foo()
        {
            return 3;
        }

        public static int FooBar(Dummy dummy)
        {
            return dummy.Foo();
        }
    }

    public class Bar
    {
        public void Baz()
        {
//#0025
//-            var dummy = ''new Dummy()'';
//+            Dummy dummy = new();

//#0025
//-            Dummy.FooBar(''new Dummy()'');
//+            Dummy.FooBar(new());

            /*var a = new Dummy(); //Change.
            new Dummy(); //Dont change.
            Dummy.FooBar(new Dummy()); //Change.*/

            /*Dummy dummy2 = new();
            Dummy dummy3 = new Dummy();
            var foo = dummy.Foo();
            var dummyFooBar = dummy.FooBar(dummy);
            int fooBar = dummy.FooBar(dummy);*/
        }
    }
}
