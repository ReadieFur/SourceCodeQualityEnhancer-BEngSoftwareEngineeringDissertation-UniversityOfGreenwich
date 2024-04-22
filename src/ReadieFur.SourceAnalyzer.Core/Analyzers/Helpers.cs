using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    internal static class Helpers
    {
        //https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/choosing-diagnostic-ids?source=recommendations
        public const string ANALYZER_ID_PREFIX = "RFSA"; //ReadieFur Source Analyzer

        public static bool TryGetAnalyzerType(string analyzerID, out EAnalyzerID enumValue)
        {
            enumValue = default;

            //https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum
            if (analyzerID.Length < ANALYZER_ID_PREFIX.Length + 4
                || !analyzerID.StartsWith(ANALYZER_ID_PREFIX)
                || !int.TryParse(analyzerID.Substring(ANALYZER_ID_PREFIX.Length), out int value)
                || !Enum.IsDefined(typeof(EAnalyzerID), value))
                return false;

            enumValue = (EAnalyzerID)(object)value;
            return true;
        }

        public static DiagnosticSeverity ToDiagnosticSeverity(this ESeverity severity)
        {
            return severity switch
            {
                ESeverity.None => DiagnosticSeverity.Hidden,
                ESeverity.Info => DiagnosticSeverity.Info,
                ESeverity.Warning => DiagnosticSeverity.Warning,
                ESeverity.Error => DiagnosticSeverity.Error,
                _ => throw new InvalidOperationException()
            };
        }

        public static DiagnosticSeverity GetDiagnosticSeverity(ESeverity? severity)
        {
            if (!severity.HasValue)
                return DiagnosticSeverity.Hidden;
            else
                return severity.Value.ToDiagnosticSeverity();
        }

        public static string ToTag(this EAnalyzerID self)
        {
            return ANALYZER_ID_PREFIX + ((int)self).ToString().PadLeft(4, '0');
        }

        public static async Task<int> GetIndentationLevelForPositionAsync(SyntaxTree syntaxTree, LinePosition position, CancellationToken cancellationToken = default)
        {
            #region Get all syntax by line
            Dictionary<int, List<SyntaxNodeTokenOrTrivia>> lines = new();

            foreach (SyntaxToken syntaxToken in (await syntaxTree.GetRootAsync(cancellationToken)).DescendantTokens(_ => true, true))
            {
                int tokenLine = syntaxToken.GetLocation().GetLineSpan().StartLinePosition.Line;

                if (!lines.ContainsKey(tokenLine))
                    lines.Add(tokenLine, new());

                foreach (SyntaxTrivia syntaxTrivia in syntaxToken.LeadingTrivia)
                {
                    int triviaLine = syntaxTrivia.GetLocation().GetLineSpan().StartLinePosition.Line;

                    if (!lines.ContainsKey(triviaLine))
                        lines.Add(triviaLine, new());

                    lines[triviaLine].Add(syntaxTrivia);
                }

                lines[tokenLine].Add(syntaxToken);

                foreach (SyntaxTrivia syntaxTrivia in syntaxToken.TrailingTrivia)
                {
                    int triviaLine = syntaxTrivia.GetLocation().GetLineSpan().StartLinePosition.Line;

                    if (!lines.ContainsKey(triviaLine))
                        lines.Add(triviaLine, new());

                    lines[triviaLine].Add(syntaxTrivia);
                }
            }
            #endregion

            #region Calculate indentation
            IReadOnlyDictionary<SyntaxKind, SyntaxKind> tokens = new Dictionary<SyntaxKind, SyntaxKind>
            {
                { SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken },
                { SyntaxKind.OpenBracketToken, SyntaxKind.CloseBracketToken },
                { SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken }
            };

            Stack<SyntaxKind> tokenStack = new();

            //foreach (SyntaxNodeTokenOrTrivia syntax in lines.OrderBy(kvp => kvp.Key).SelectMany(kvp => kvp.Value))
            foreach (SyntaxNodeTokenOrTrivia syntax in lines.SelectMany(kvp => kvp.Value).OrderBy(s => s.GetLocation().SourceSpan.Start))
            {
                SyntaxToken token;
                SyntaxKind kind;

                //Skip nodes as they can contain duplicate tokens.
                if (syntax.IsNode)
                {
                    continue;
                    token = syntax.Node.GetFirstToken();
                    kind = token.Kind();
                }
                else if (syntax.IsToken)
                {
                    token = syntax.Token;
                    kind = token.Kind();
                }
                else //if (syntax.IsTrivia)
                {
                    continue;
                    token = syntax.Trivia.Token;
                    kind = token.Kind();
                }

                if (tokens.Keys.Contains(kind))
                {
                    tokenStack.Push(kind);
                }

                if (token.GetLocation().GetLineSpan().StartLinePosition >= position)
                    break;

                if (tokens.Values.Contains(kind))
                {
                    if (tokenStack.Count() == 0)
                        throw new InvalidOperationException("Mismatched tokens");

                    if (tokens[tokenStack.Peek()] == kind)
                        tokenStack.Pop();
                    else
                        throw new InvalidOperationException("Mismatched tokens");
                }
            }

            return tokenStack.Count();
            #endregion
        }

        public static int IndentationLevelToSpaces(int indentationLevel)
        {
            return indentationLevel * (ConfigManager.Configuration.Formatting?.Indentation?.Size ?? Indentation.DEFAULT_SIZE);
        }
    }
}
