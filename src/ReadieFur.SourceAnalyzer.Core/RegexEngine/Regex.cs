using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    /*References:
     * https://kean.blog/post/lets-build-regex
     * https://web.mit.edu/6.005/www/fa15/classes/17-regex-grammars/
     * https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_expressions/Cheatsheet
     * https://en.wikipedia.org/wiki/Regular_expression
     * https://github.com/firasdib/Regex101
     */
    public class Regex
    {
        public readonly string Pattern;

        private readonly Token _root;

        public Regex(string pattern)
        {
            Pattern = pattern;

            //Specify the token types that we will be using. This MUST be in order of the process we want to check against.
            Type[] tokenTypes =
            [
                //typeof(Anchor),
                typeof(GroupConstruct),
                typeof(CharacterClass),
            ];

            //Create a copy of the string so we can modify it.
            string refPattern = pattern.Substring(0);
            //For now I will wrap whatever pattern is given into a group construct (helps with the initial parsing).
            refPattern = $"({refPattern})";

            //Attempt to find a valid parser for the root of the pattern.
            foreach (Type type in tokenTypes)
            {
                //Check if the pattern can be parsed.
                _root = (Token)Activator.CreateInstance(type);
                if (_root.CanParse(ref refPattern) is not null)
                    break;
            }

            if (_root is null)
                throw new InvalidOperationException("Invalid pattern.");

            //Build the token tree.
            _root.Parse(ref refPattern);
        }

        public bool Test(string input)
        {
            int index = 0;
            return _root.Test(input, ref index);
        }

        //The built-in C# Regex library can replace text in a string however it does not quite meet the complexity requirements that I have for text transformation.
        public string Conform(string input, SConformOptions options = new())
        {
            int index = 0;
            string output = "";
            if (!_root.Conform(input, ref index, ref output, options))
                throw new Exception();
            return output;
        }
    }
}
