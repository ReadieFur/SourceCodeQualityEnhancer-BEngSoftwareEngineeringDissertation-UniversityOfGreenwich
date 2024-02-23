using System;
using System.Linq;

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

            char c0 = consumablePattern[0];
            if (c0 == '\\')
            {
                if (consumablePattern.Length == 1)
                    throw new InvalidOperationException();

                Value = consumablePattern[1];
                IsEscaped = true;
            }
            else if (new[] { '{', '}', '(', ')', '[', ']' }.Any(c => c == c0))
            {
                throw new InvalidOperationException($"Character '{c0}' must be escaped");
            }
            else
            {
                Value = c0;
                IsEscaped = false;
            }

            return this;
        }

        public override Token Parse(ref string consumablePattern)
        {
            string startingPattern = consumablePattern;

            consumablePattern = consumablePattern.Substring(IsEscaped ? 2 : 1);

            Pattern = startingPattern.Substring(0, startingPattern.Length - consumablePattern.Length);

            Token endToken = this;
            endToken = Quantifier.CheckForQuantifier(ref consumablePattern, this, endToken);

            return endToken;
        }

        public override bool Test(string input, ref int index)
        {
            return Read(input, ref index, 1)[0] == Value;
        }

        public override bool Conform(ConformParameters parameters)
        {
            char c = Read(parameters.Input, ref parameters.Index, 1)[0];
            
            //If the char is the same as the atom but the case is different then change the case, otherwise throw an exception.
            if (char.IsLetter(c) && char.IsLetter(Value) && char.ToLower(c) == char.ToLower(Value))
            {
                if (char.IsUpper(c) && char.IsLower(Value))
                    parameters.Output += char.ToUpper(Value);
                else if (char.IsLower(c) && char.IsUpper(Value))
                    parameters.Output += char.ToLower(Value);

                return true;
            }
            else if (parameters.Options.InsertLiterals)
            {
                parameters.Output += Value;
                parameters.LastInsertedLiteral = parameters.Index;
                parameters.Index--; //Decrement the index because we didn't consume a value, instead we inserted one.
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
