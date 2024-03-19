using System.Collections.Generic;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    //Structs are not complex enough for my use case.
    //Records are not supported with the C# framework I am using.
    public class SConformOptions
    {
        /// <summary>
        /// Used to set the global options for the quantifiers.
        /// </summary>
        public ConformQuantifierOptions GlobalQuantifierOptions { get; set; } = new();

        /// <summary>
        /// Used to set the options for quantifiers based on their depth in the token tree.
        /// </summary>
        public Dictionary<int, ConformQuantifierOptions> QuanitifierOptions { get; set; } = new();

        /// <summary>
        /// Weathers or not to insert mismatched literals into the output string.
        /// </summary>
        public bool InsertLiterals { get; set; } = false;

        /// <summary>
        /// The default, reccomended options for the conform engine (works for most cases).
        /// </summary>
        public static SConformOptions Default => new()
        {
            GlobalQuantifierOptions = new()
            {
                GreedyQuantifierDelimiters = ['_'],
                GreedyQuantifiersSplitOnCaseChangeToUpper = true
            },
            InsertLiterals = true
        };
    }
}
