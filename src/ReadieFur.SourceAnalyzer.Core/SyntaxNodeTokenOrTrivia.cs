using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Core
{
    public struct SyntaxNodeTokenOrTrivia
    {
        private SyntaxNode? _underlyingNode { get; set; }
        private SyntaxToken? _underlyingToken { get; set; }
        private SyntaxTrivia? _underlyingTrivia { get; set; }

        public SyntaxNode Node => _underlyingNode ?? throw new InvalidOperationException("Not a node.");
        public SyntaxToken Token => _underlyingToken ?? throw new InvalidOperationException("Not a token.");
        public SyntaxTrivia Trivia => _underlyingTrivia ?? throw new InvalidOperationException("Not a trivia.");

        public bool IsNode => _underlyingNode is SyntaxNode;
        public bool IsToken => _underlyingToken is SyntaxToken;
        public bool IsTrivia => _underlyingTrivia is SyntaxTrivia;

        public SyntaxNodeTokenOrTrivia(SyntaxNode node)
        {
            _underlyingNode = node;
        }

        public SyntaxNodeTokenOrTrivia(SyntaxToken token)
        {
            _underlyingToken = token;
        }

        public SyntaxNodeTokenOrTrivia(SyntaxTrivia trivia)
        {
            _underlyingTrivia = trivia;
        }

        public static implicit operator SyntaxNode(SyntaxNodeTokenOrTrivia node) => node.Node;
        public static implicit operator SyntaxToken(SyntaxNodeTokenOrTrivia token) => token.Token;
        public static implicit operator SyntaxTrivia(SyntaxNodeTokenOrTrivia trivia) => trivia.Trivia;

        public static implicit operator SyntaxNodeTokenOrTrivia(SyntaxNode node) => new(node);
        public static implicit operator SyntaxNodeTokenOrTrivia(SyntaxToken token) => new(token);
        public static implicit operator SyntaxNodeTokenOrTrivia(SyntaxTrivia trivia) => new(trivia);

        public Location GetLocation()
        {
            if (IsNode)
                return Node.GetLocation();
            else if (IsToken)
                return Token.GetLocation();
            else if (IsTrivia)
                return Trivia.GetLocation();
            throw new InvalidOperationException("Not a node, token or trivia.");
        }

        public override string ToString()
        {
            if (IsNode)
                return Node.ToString();
            else if (IsToken)
                return Token.ToString();
            else if (IsTrivia)
                return Trivia.ToString();
            throw new InvalidOperationException("Not a node, token or trivia.");
        }
    }
}
