using ReadieFur.SourceAnalyzer.Core.RegexEngine;

namespace ReadieFur.SourceAnalyzer.UnitTests.RegexEngine
{
    internal static class Common
    {
        public const string PATTERN_PASCAL_CASE = "([A-Z][a-z]+)+";
        public const string PATTERN_CAMEL_CASE = "[a-z]+([A-Z][a-z]+)*";
        public const string PATTERN_SNAKE_CASE = "[a-z]+(_[a-z]+)*";

        public const string INPUT_PASCAL_CASE = "PascalCase";
        public const string INPUT_CAMEL_CASE = "camelCase";
        public const string INPUT_SNAKE_CASE = "snake_case";
        public const string INPUT_LOWERCASE = "lowercase";
        public const string INPUT_UPPERCASE = "UPPERCASE";
        public const string INPUT_DOUBLE_UPPERCASE = "DOUBLE_UPPERCASE";

        public static SConformOptions CONFORM_OPTIONS
        {
            get
            {
                return new()
                {
                    GlobalQuantifierOptions = new()
                    {
                        GreedyQuantifierDelimiters = new[] { '_' },
                        GreedyQuantifiersSplitOnCaseChange = true
                    },
                    /*QuanitifierOptions = new()
                    {
                        {
                            0, //Top level quantifier
                            new(false)
                            {
                                GreedyQuantifiersSplitOnCaseChange = false //We dont want to split on case changes for the top level quantifier due to patterns often having a custom first match.
                            }
                        }
                    },*/
                    InsertLiterals = true
                };
            }
        }
    }
}
