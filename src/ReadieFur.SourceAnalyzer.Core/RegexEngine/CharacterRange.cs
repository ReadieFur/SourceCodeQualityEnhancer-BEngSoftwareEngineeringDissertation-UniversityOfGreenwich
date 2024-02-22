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
            endToken = Quantifier.CheckForQuantifier(ref consumablePattern, this, endToken);

            return endToken;
        }

        public override bool Test(string input, ref int index)
        {
            int c;
            try { c = Read(input, ref index, 1)[0]; }
            catch (IndexOutOfRangeException) { return true; } //If we are at the end of the string, return true.
            return c >= _start && c <= _end;
        }
    }
}
