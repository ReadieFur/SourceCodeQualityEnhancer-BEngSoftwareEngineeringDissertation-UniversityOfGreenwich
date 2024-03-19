using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    //Anchors ignored for now.
    internal class Anchor : AToken
    {
        public override AToken CanParse(ref string consumablePattern)
        {
            throw new NotImplementedException();
        }

        public override AToken Parse(ref string consumablePattern)
        {
            throw new NotImplementedException();
        }

        public override bool Test(string input, ref int index)
        {
            throw new NotImplementedException();
        }

        public override bool Conform(ConformParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
