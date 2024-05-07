using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    public class ConformQuantifierOptions
    {
        //These should NEVER be null under normal circumstances, only for object comparison.
        public IReadOnlyCollection<char>? GreedyQuantifierDelimiters { get; set; }
        public bool? GreedyQuantifiersSplitOnCaseChangeToUpper { get; set; }
        public bool? GreedyQuantifiersSplitOnCaseChangeToLower { get; set; }
        public bool? GreedyQuantifiersSplitOnAlphanumericChange { get; set; }

        public ConformQuantifierOptions()
        {
            GreedyQuantifierDelimiters = new List<char>();
            GreedyQuantifiersSplitOnCaseChangeToUpper = false;
            GreedyQuantifiersSplitOnCaseChangeToLower = false;
            GreedyQuantifiersSplitOnAlphanumericChange = false;
        }

        /// <param name="extendDefaults">Placeholder value, set to false to set "override" properties (setting unused values to null).</param>
        public ConformQuantifierOptions(bool extendDefaults)
        {
            if (extendDefaults)
                return;

            foreach (PropertyInfo property in GetType().GetProperties())
                property.SetValue(this, null);
        }
    }
}
