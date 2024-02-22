using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal class Atom : Token
    {
        private bool IsEscaped;
        private char Value;

        public override Token? CanParse(ref string consumablePattern)
        {
            if (consumablePattern.Length == 0)
                return null;

            if (consumablePattern[0] == '\\')
            {
                if (consumablePattern.Length == 1)
                    throw new InvalidOperationException();

                Value = consumablePattern[1];
                IsEscaped = true;
            }
            else
            {
                Value = consumablePattern[0];
                IsEscaped = false;
            }

            return this;
        }

        public override Token Parse(ref string consumablePattern)
        {
            consumablePattern = consumablePattern.Substring(IsEscaped ? 2 : 1);

            Token endToken = this;
            endToken = Quantifier.CheckForQuantifier(ref consumablePattern, this, endToken);

            return endToken;
        }

        public override bool Test(string input, ref int index)
        {
            return Read(input, ref index, 1)[0] == Value;
        }

        public override bool Conform(string input, ref int index, ref string output)
        {
            char c = Read(input, ref index, 1)[0];
            
            //If the char is the same as the atom but the case is different then change the case, otherwise throw an exception.
            if (char.IsLetter(c) && char.IsLetter(Value) && char.ToLower(c) == char.ToLower(Value))
            {
                if (char.IsUpper(c) && char.IsLower(Value))
                    output += char.ToUpper(Value);
                else if (char.IsLower(c) && char.IsUpper(Value))
                    output += char.ToLower(Value);

                return true;
            }
            else
            {
                index--;
                return false;
            }
        }
    }
}
