using ReadieFur.SourceAnalyzer.Core;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal struct SyntaxTreeLine
    {
        public int LineNumber { get; private set; }
        public IReadOnlyList<SyntaxNodeTokenOrTrivia> Syntax { get; private set; }

        public SyntaxTreeLine(int lineNumber, List<SyntaxNodeTokenOrTrivia> syntax)
        {
            LineNumber = lineNumber;
            Syntax = syntax;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (SyntaxNodeTokenOrTrivia nodeTokenOrTrivia in Syntax)
            {
                if (nodeTokenOrTrivia.IsNode)
                    sb.Append(nodeTokenOrTrivia.Node.ToString());
                else if (nodeTokenOrTrivia.IsToken)
                    sb.Append(nodeTokenOrTrivia.Token.ToString());
                else if (nodeTokenOrTrivia.IsTrivia)
                    sb.Append(nodeTokenOrTrivia.Trivia.ToString());
            }
            return sb.ToString();
        }
    }
}
