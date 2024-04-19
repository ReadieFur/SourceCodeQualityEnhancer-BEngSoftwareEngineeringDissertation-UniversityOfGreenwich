﻿
namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public sealed class Formatting
    {
        public CurlyBraces CurlyBraces { get; set; } = new();
        public Indentation Indentation { get; set; } = new();
        public Comments Comments { get; set; } = new();
    }
}
