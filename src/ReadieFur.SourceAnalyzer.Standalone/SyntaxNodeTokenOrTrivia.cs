using Microsoft.CodeAnalysis;
using System;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    internal struct SyntaxNodeTokenOrTrivia
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
    }
}
