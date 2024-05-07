using System.Collections.Generic;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal class DiffBlock
    {
        public EDiffBlock Type { get; private set; }
        public IReadOnlyList<SyntaxTreeLine> Lines { get; private set; }

        public DiffBlock(EDiffBlock type, List<SyntaxTreeLine> lines)
        {
            Type = type;
            Lines = lines;
        }
    }
}
