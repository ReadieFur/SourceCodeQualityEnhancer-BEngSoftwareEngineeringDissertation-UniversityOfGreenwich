using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    //Anchors ignored for now.
    internal class Anchor : Token
    {
        public override Token CanParse(ref string consumablePattern)
        {
            throw new NotImplementedException();
        }

        public override Token Parse(ref string consumablePattern)
        {
            throw new NotImplementedException();
        }

        public override bool Test(string input, ref int index)
        {
            throw new NotImplementedException();
        }

        public override bool Conform(string input, ref int index, ref string output)
        {
            throw new NotImplementedException();
        }
    }
}
