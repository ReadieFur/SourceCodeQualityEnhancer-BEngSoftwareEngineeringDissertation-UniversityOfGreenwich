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
            return Read(input, ref index, 1)[0] == Value;
        }
    }
}
