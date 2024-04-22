using System;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class Inferred
    {
        public InferredBlock? This { get; set; }
        public InferredBlock? AccessModifier { get; set; }
        public InferredBlock? Constructor { get; set; }
        public InferredBlock? Type { get; set; }
    }
}
