using System;
using System.Collections.Generic;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal abstract class Token
    {
        public Token? Parent { get; set; }
        public List<Token> Children { get; set; } = new();
        public Token? Previous { get; set; }
        public Token? Next { get; set; }

        /// <summary>
        /// Begins the first part of the parsing process, if the method believes it can parse the pattern, it will return a token, otherwise it will return null.
        /// </summary>
        /// <param name="consumablePattern"></param>
        /// <returns></returns>
        public abstract Token? CanParse(ref string consumablePattern);

        /// <summary>
        /// Recursively builds the token tree.
        /// </summary>
        /// <param name="consumablePattern"></param>
        /// <returns>The token at the end of the tree.</returns>
        public abstract Token Parse(ref string consumablePattern);

        /// <summary>
        /// Rather than using a consumable here we will use a positional index so we can work with lookaheads and lookbehinds.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <returns>TODO: Have this return a list of "Group" objects</returns>
        public abstract bool Test(string input, ref int index);

        /// <summary>
        /// Modifies the input string to conform to the token.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="output"></param>
        public abstract bool Conform(string input, ref int index, ref string output, SConformOptions options);

        /// <summary>
        /// Obtains a substring from the input string and modifies the index.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected string Read(string input, ref int index, int length)
        {
            if (index + length > input.Length)
                throw new IndexOutOfRangeException();
            string data = input.Substring(index, length);
            index += length;
            return data;
        }

        /// <summary>
        /// Recursively parses a group of tokens.
        /// </summary>
        /// <param name="types"></param>
        /// <param name="consumablePattern"></param>
        /// <param name="returnOnChar"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected Token RecursiveParse(Type[] types, ref string consumablePattern, char returnOnChar)
        {
            //Iterate over each item in the group until we find the closing parenthesis.
            bool isValid = false;
            Token endToken = this;
            while (consumablePattern.Length > 0)
            {
                if (consumablePattern[0] == returnOnChar)
                {
                    isValid = true;
                    consumablePattern = consumablePattern.Substring(1);
                    break;
                }

                Token? token = null;
                foreach (Type type in types)
                {
                    token = (Token)Activator.CreateInstance(type);
                    if (token.CanParse(ref consumablePattern) is not null)
                        break;
                }
                if (token is null)
                    throw new InvalidOperationException();

                Children.Add(token);
                token.Parent = this;
                token.Previous = endToken;
                endToken.Next = token;
                endToken = token.Parse(ref consumablePattern);
            }
            if (!isValid)
                throw new InvalidOperationException();

            return endToken;
        }
    }
}
