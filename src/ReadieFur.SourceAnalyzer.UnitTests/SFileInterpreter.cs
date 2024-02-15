using ReadieFur.SourceAnalyzer.UnitTests.Analyzers;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    internal struct SFileInterpreter
    {
        public Type sourceType { get; private set; }
        public string sourceText { get; private set; }
        public string interpretedText { get; private set; }
        public string codeFixText { get; private set; }

        public static async Task<SFileInterpreter> Interpret(Type sourceType)
        {
            string sourceText = await GetSourceFile(sourceType);

            #region Interpret file
            //Remove custom usings (should only contain base references).
            string interpretedText = sourceText
                .Split(Environment.NewLine)
                .SkipWhile(l => l.StartsWith($"using {nameof(ReadieFur)}"))
                .Aggregate((a, b) => a + Environment.NewLine + b);

            string attribute = $"[{nameof(NamingAnalyzerAttribute).Substring(0, nameof(NamingAnalyzerAttribute).Length - 9)}]";

            string substring = interpretedText;
            List<int> attributePositions = new();
            //Search over the source text in reverse (makes it easier to replace the text).
            int index = 0;
            while ((index = substring.LastIndexOf(attribute, StringComparison.Ordinal)) != -1)
            {
                attributePositions.Add(index);
                substring = substring.Substring(0, index);
            }

            string codeFixText = interpretedText;
            foreach (int position in attributePositions)
            {
                //TODO: Interpret the text.

                //Replace the attribute with a blank string.
                interpretedText = interpretedText.Remove(position, attribute.Length);
            }
            #endregion

            return new SFileInterpreter
            {
                sourceType = sourceType,
                sourceText = sourceText,
                interpretedText = interpretedText,
                codeFixText = codeFixText
            };
        }
    }
}
