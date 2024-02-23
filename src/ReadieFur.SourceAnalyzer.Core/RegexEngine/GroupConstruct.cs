using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal class GroupConstruct : Token
    {
        //For now the commented out fields are unsupported.
        private enum GroupType
        {
            WithId,
            WithoutId = WithId,
            NonCapturing = WithoutId,
            ReuseID = WithoutId,
            //Comment,
            //ConditionalLookahead,
            //ConditionalLookbehind,
            PositiveLookahead,
            PositiveLookbehind,
            NegativeLookahead,
            NegativeLookbehind
        }

        private GroupType _type;
        private int _toConsume = 0;

        public override Token? CanParse(ref string consumablePattern)
        {
            //First try to get the modifiers for this group.
            if (consumablePattern.StartsWith("(?<!"))
            {
                _type = GroupType.NegativeLookbehind;
                _toConsume = 4;
            }
            else if (consumablePattern.StartsWith("(?<="))
            {
                _type = GroupType.PositiveLookbehind;
                _toConsume = 4;
            }
            else if (consumablePattern.StartsWith("(?="))
            {
                _type = GroupType.PositiveLookahead;
                _toConsume = 3;
            }
            else if (consumablePattern.StartsWith("(?!"))
            {
                _type = GroupType.NegativeLookahead;
                _toConsume = 3;
            }
            //else if (consumablePattern.StartsWith("(?(?<=")) Type = GroupType.ConditionalLookbehind;
            //else if (consumablePattern.StartsWith("(?(?=")) Type = GroupType.ConditionalLookahead;
            //else if (consumablePattern.StartsWith("(?#")) Type = GroupType.Comment;
            else if (consumablePattern.StartsWith("(?|"))
            {
                _type = GroupType.ReuseID;
                _toConsume = 3;
            }
            else if (consumablePattern.StartsWith("(?>"))
            {
                _type = GroupType.NonCapturing;
                _toConsume = 3;
            }
            else if (consumablePattern.StartsWith("(?:"))
            {
                _type = GroupType.WithoutId;
                _toConsume = 3;
            }
            else if (consumablePattern.StartsWith("("))
            {
                _type = GroupType.WithId;
                _toConsume = 1;
            }
            else
            {
                return null;
            }

            return this;
        }

        public override Token Parse(ref string consumablePattern)
        {
            //Work on the string iteratively.
            //Try to get the group contents.
            //When we encounter a closing parenthesis, we can return from this method.
            //I am using a consumable string so that subsequent calls to Parse can consume from the pattern so when that method returns we pick up from where that method left off.

            string startingPattern = consumablePattern;

            consumablePattern = consumablePattern.Substring(_toConsume);

            //Iterate over each item in the group until we find the closing parenthesis.
            Token endToken = RecursiveParse(
            [
                typeof(GroupConstruct),
                typeof(CharacterClass),
                typeof(MetaSequence),
                typeof(Atom),
            ], ref consumablePattern, ')');

            Pattern = startingPattern.Substring(0, startingPattern.Length - consumablePattern.Length);

            endToken = Quantifier.CheckForQuantifier(ref consumablePattern, this, endToken);

            return endToken;
        }

        public override bool Test(string input, ref int index)
        {
            //Depending on the group type we may need to either look ahead of behind before we can determine the result of the test.
            bool invert = _type switch
            {
                GroupType.NegativeLookahead or
                GroupType.NegativeLookbehind => true,
                _ => false,
            };

            //Test each token in the group.
            foreach (Token token in Children)
            {
                bool result = token.Test(input, ref index);
                if (invert)
                    result = !result;
                if (!result)
                    return false;
            }

            return true;
        }

        public override bool Conform(ConformParameters parameters)
        {
            //Depending on the group type we may need to either look ahead of behind before we can determine the result of the test.
            bool invert = _type switch
            {
                GroupType.NegativeLookahead or
                GroupType.NegativeLookbehind => true,
                _ => false,
            };

            //Test each token in the group.
            foreach (Token token in Children)
            {
                bool result = token.Conform(parameters);
                if (invert)
                    result = !result;
                if (!result)
                    return false;
            }

            return true;
        }
    }
}
