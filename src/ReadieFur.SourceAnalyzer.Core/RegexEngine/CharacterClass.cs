using System;
using YamlDotNet.Core.Tokens;

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
    }
}
