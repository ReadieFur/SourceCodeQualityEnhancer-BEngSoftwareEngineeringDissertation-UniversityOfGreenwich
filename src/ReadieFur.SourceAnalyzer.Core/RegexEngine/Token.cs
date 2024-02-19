using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal abstract class Token
    {
#if DEBUG
        //Id for easier debugging purposes.
        private readonly Guid _id = Guid.NewGuid();
#endif

        public Token? Parent { get; set; }
        public Token? Previous { get; set; }
        public Token? Next { get; set; }
    }
}
