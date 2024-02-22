using System.Collections.Generic;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    public struct SConformOptions
    {
        public IReadOnlyCollection<char> GreedyQuantifierDelimiters { get; set; }
        public bool GreedyQuantifiersSplitOnCaseChange { get; set; }
        public bool GreedyQuantifiersSplitOnAlphanumericChange { get; set; }
    }
}
