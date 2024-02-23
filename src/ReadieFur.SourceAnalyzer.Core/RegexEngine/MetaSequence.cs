using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal class MetaSequence : Token
    {
        [Flags]
        private enum MetaSequenceType
        {
            Invert = 1 << 1,
            Whitespace = 1 << 2,
            Digit = 1 << 3,
            Word = 1 << 4, //Letter or digit (i.e. not a special character)
            //Unicode = 1 << 5,
            //Dataunit = 1 << 6,
            Character = 1 << 7,
        }

        private MetaSequenceType Type { get; set; }

        public override Token? CanParse(ref string consumablePattern)
        {
            if (consumablePattern.StartsWith("."))
            {
                Type = MetaSequenceType.Character;
                return this;
            }

            if (!consumablePattern.StartsWith("\\") || consumablePattern.Length < 2)
                return null;

            switch (consumablePattern[1])
            {
                case 's':
                    Type = MetaSequenceType.Whitespace;
                    break;
                case 'S':
                    Type = MetaSequenceType.Whitespace | MetaSequenceType.Invert;
                    break;
                case 'd':
                    Type = MetaSequenceType.Digit;
                    break;
                case 'D':
                    Type = MetaSequenceType.Digit | MetaSequenceType.Invert;
                    break;
                case 'w':
                    Type = MetaSequenceType.Word;
                    break;
                case 'W':
                    Type = MetaSequenceType.Word | MetaSequenceType.Invert;
                    break;
                /*case 'X':
                    Type = MetaSequenceType.Unicode;
                    break;*/
                /*case 'C':
                    Type = MetaSequenceType.Dataunit;
                    break;*/
                default:
                    return null;
            }

            return this;
        }

        public override Token Parse(ref string consumablePattern)
        {
            string startingPattern = consumablePattern;

            consumablePattern = consumablePattern.Substring(Type.HasFlag(MetaSequenceType.Character) ? 1 : 2);

            Pattern = startingPattern.Substring(0, startingPattern.Length - consumablePattern.Length);

            Token endToken = this;
            endToken = Quantifier.CheckForQuantifier(ref consumablePattern, this, endToken);

            return endToken;
        }

        public override bool Test(string input, ref int index)
        {
            char c = Read(input, ref index, 1)[0];

            bool result = false;

            if (Type.HasFlag(MetaSequenceType.Whitespace))
                result = char.IsWhiteSpace(c);
            else if (Type.HasFlag(MetaSequenceType.Digit))
                result = char.IsDigit(c);
            else if (Type.HasFlag(MetaSequenceType.Word))
                result = char.IsLetterOrDigit(c);
            /*else if (Type.HasFlag(MetaSequenceType.Unicode))
                result = char.GetUnicodeCategory(c) == UnicodeCategory.OtherNotAssigned;*/
            /*else if (Type.HasFlag(MetaSequenceType.Dataunit))
                result = char.GetUnicodeCategory(c) == UnicodeCategory.OtherNotAssigned;*/

            if (Type.HasFlag(MetaSequenceType.Invert))
                result = !result;

            return result;
        }

        public override bool Conform(ConformParameters parameters)
        {
            //As far as I can tell not much can be done here for character transformation.
            return Test(parameters.Input, ref parameters.Index);
        }
    }
}
