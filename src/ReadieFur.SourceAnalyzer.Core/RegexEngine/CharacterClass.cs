using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    //For the sake of my simple Regex engine, this Token will not be able to traverse any further into group constructs.
    internal class CharacterClass : Token
    {
        private bool _negated = false;
        //private List<Token> _tokens = new();

        public override Token? CanParse(ref string consumablePattern)
        {
            return consumablePattern.StartsWith("[") ? this : null;
        }

        public override Token Parse(ref string consumablePattern)
        {
            string startingPattern = consumablePattern;

            if (consumablePattern.StartsWith("[^"))
            {
                _negated = true;
                consumablePattern = consumablePattern.Substring(2);
            }
            else //if (consumablePattern.StartsWith("["))
            {
                consumablePattern = consumablePattern.Substring(1);
            }

            if (consumablePattern.Length == 0)
                throw new InvalidOperationException();

            Token endToken = RecursiveParse(
            [
                typeof(CharacterRange),
                typeof(MetaSequence),
                typeof(Atom),
            ], ref consumablePattern, ']');

            Pattern = startingPattern.Substring(0, startingPattern.Length - consumablePattern.Length);

            endToken = Quantifier.CheckForQuantifier(ref consumablePattern, this, endToken);

            return endToken;
        }

        public override bool Test(string input, ref int index)
        {
            foreach (Token token in Children)
            {
                bool result = token.Test(input, ref index);
                if (_negated)
                    result = !result;
                if (!result)
                    return false;
            }
            return true;
        }

        public override bool Conform(ConformParameters parameters)
        {
            foreach (Token token in Children)
            {
                bool result = token.Conform(parameters);
                if (_negated)
                    result = !result;
                if (!result)
                    return false;
            }
            return true;
        }
    }
}
