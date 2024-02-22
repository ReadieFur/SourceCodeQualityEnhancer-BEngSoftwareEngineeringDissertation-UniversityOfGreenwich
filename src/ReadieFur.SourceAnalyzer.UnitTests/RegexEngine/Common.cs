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
    }
}
