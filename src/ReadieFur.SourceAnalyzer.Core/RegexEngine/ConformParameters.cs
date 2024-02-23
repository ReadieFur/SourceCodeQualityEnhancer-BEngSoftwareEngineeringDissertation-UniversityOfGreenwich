namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    /// <summary>
    /// A wrapper for the parameters used in the conform method.
    /// Helps with the state management of the conform method as well as the readability and maintainability of the code.
    /// </summary>
    internal class ConformParameters
    {
        public readonly string Input;
        public readonly SConformOptions Options = new();
        public int Index = 0;
        public string Output = "";
        public int LastGreedyQuantifiersSplitOnCaseChange = -1;

        public ConformParameters(string input)
        {
            Input = input;
        }

        public ConformParameters(string input, SConformOptions options)
        {
            Input = input;
            Options = options;
        }
    }
}
