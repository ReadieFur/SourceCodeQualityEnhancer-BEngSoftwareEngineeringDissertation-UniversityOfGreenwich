using System;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class Punctuation
    {
        public List<AroundToken>? SpaceAround { get; set; }
        public List<AroundToken>? NewLine { get; set; }
    }
}
