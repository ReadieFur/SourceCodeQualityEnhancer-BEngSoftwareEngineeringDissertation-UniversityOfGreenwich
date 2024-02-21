using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal class CharacterRange : Token
    {
        //Store character as its ASCII value for easier comparison.
        private int _start;
        private int _end;

        public override Token? CanParse(ref string consumablePattern)
        {
            return consumablePattern.Length >= 3 && consumablePattern[1] == '-' ? this : null;
        }

        public override Token Parse(ref string consumablePattern)
        {
            _start = consumablePattern[0];
            _end = consumablePattern[2];
            consumablePattern = consumablePattern.Substring(3);

            if (_start > _end)
                throw new InvalidOperationException();

            Token endToken = this;
            //Check if there are any quantifiers that could be applied to this group.
            Token quantifier = new Quantifier().CanParse(ref consumablePattern);
            if (quantifier is not null)
            {
                Children.Add(quantifier);
                quantifier.Parent = this;
                quantifier.Previous = endToken;
                endToken.Next = quantifier;
                endToken = quantifier.Parse(ref consumablePattern);
            }

            return endToken;
        }

        public override bool Test(string input, ref int index)
        {
            int c = Read(input, ref index, 1)[0];
            return c >= _start && c <= _end;
        }
    }
}
