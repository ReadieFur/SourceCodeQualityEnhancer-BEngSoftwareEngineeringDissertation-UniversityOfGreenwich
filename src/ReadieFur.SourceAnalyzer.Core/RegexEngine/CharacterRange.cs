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
            int c = Read(input, ref index, 1)[0];
            return c >= _start && c <= _end;
        }

        public override bool Conform(string input, ref int index, ref string output)
        {
            char c = Read(input, ref index, 1)[0];

            //If the character is already in the range then just add it to the output.
            if (c >= _start && c <= _end)
            {
                output += c;
                return true;
            }

            //Otherwise we need to check if the provided character can be transformed.
            //The following operation is only applicable to letters.
            if (!char.IsLetter(c))
            {
                //index--;
                return false;
            }
            
            //Check if the character is valid by changing the case.
            c = char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
            if (c >= _start && c <= _end)
            {
                output += c;
                return true;
            }
            else
            {
                //index--;
                return false;
            }
        }
    }
}
