﻿namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public sealed class Formatting
    {
        public CurlyBraces? CurlyBraces { get; set; }
        public Indentation? Indentation { get; set; }
        public Comments? Comments { get; set; }
        public Punctuation? Punctuation { get; set; }
        public ObjectStructure? ObjectStructure { get; set; }
    }
}
