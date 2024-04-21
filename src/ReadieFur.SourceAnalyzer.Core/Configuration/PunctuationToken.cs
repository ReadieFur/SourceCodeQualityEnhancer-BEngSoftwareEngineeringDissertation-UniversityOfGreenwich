using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public class PunctuationToken : ConfigBase
    {
        [YamlIgnore] public static readonly Dictionary<string, SyntaxKind> Punctuation = new()
        {
            { @"~", SyntaxKind.TildeToken },
            { @"!", SyntaxKind.ExclamationToken },
            { @"$", SyntaxKind.DollarToken },
            { @"%", SyntaxKind.PercentToken },
            { @"^", SyntaxKind.CaretToken },
            { @"&", SyntaxKind.AmpersandToken },
            { @"*", SyntaxKind.AsteriskToken },
            { @"(", SyntaxKind.OpenParenToken },
            { @")", SyntaxKind.CloseParenToken },
            { @"-", SyntaxKind.MinusToken },
            { @"+", SyntaxKind.PlusToken },
            { @"=", SyntaxKind.EqualsToken },
            { @"{", SyntaxKind.OpenBraceToken },
            { @"}", SyntaxKind.CloseBraceToken },
            { @"[", SyntaxKind.OpenBracketToken },
            { @"]", SyntaxKind.CloseBracketToken },
            { @"|", SyntaxKind.BarToken },
            { @"\", SyntaxKind.BackslashToken },
            { @":", SyntaxKind.ColonToken },
            { @";", SyntaxKind.SemicolonToken },
            { "\"", SyntaxKind.DoubleQuoteToken },
            { @"'", SyntaxKind.SingleQuoteToken },
            { @"<", SyntaxKind.LessThanToken },
            { @",", SyntaxKind.CommaToken },
            { @">", SyntaxKind.GreaterThanToken },
            { @".", SyntaxKind.DotToken },
            { @"?", SyntaxKind.QuestionToken },
            { @"#", SyntaxKind.HashToken },
            { @"/", SyntaxKind.SlashToken },
            { @"..", SyntaxKind.DotDotToken },
            { @"||", SyntaxKind.BarBarToken },
            { @"&&", SyntaxKind.AmpersandAmpersandToken },
            { @"--", SyntaxKind.MinusMinusToken },
            { @"++", SyntaxKind.PlusPlusToken },
            { @"::", SyntaxKind.ColonColonToken },
            { @"??", SyntaxKind.QuestionQuestionToken },
            { @"->", SyntaxKind.MinusGreaterThanToken },
            { @"!=", SyntaxKind.ExclamationEqualsToken },
            { @"==", SyntaxKind.EqualsEqualsToken },
            { @"=>", SyntaxKind.EqualsGreaterThanToken },
            { @"<=", SyntaxKind.LessThanEqualsToken },
            { @"<<", SyntaxKind.LessThanLessThanToken },
            { @"<<=", SyntaxKind.LessThanLessThanEqualsToken },
            { @">=", SyntaxKind.GreaterThanEqualsToken },
            { @">>", SyntaxKind.GreaterThanGreaterThanToken },
            { @">>=", SyntaxKind.GreaterThanGreaterThanEqualsToken },
            { @"/=", SyntaxKind.SlashEqualsToken },
            { @"*=", SyntaxKind.AsteriskEqualsToken },
            { @"|=", SyntaxKind.BarEqualsToken },
            { @"&=", SyntaxKind.AmpersandEqualsToken },
            { @"+=", SyntaxKind.PlusEqualsToken },
            { @"-=", SyntaxKind.MinusEqualsToken },
            { @"^=", SyntaxKind.CaretEqualsToken },
            { @"??=", SyntaxKind.QuestionQuestionEqualsToken },
#if VSIX
            { @">>>", SyntaxKind.GreaterThanGreaterThanGreaterThanToken },
            { @">>>=", SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken },
#endif
        };

        public bool Required { get; set; } = true;

        [YamlMember(Alias = "tokens")]
        public List<string> _tokens { get; set; } = new();

        [YamlIgnore]
        public IEnumerable<SyntaxKind> Tokens => _tokens
            .Where(Punctuation.ContainsKey)
            .Select(token => Punctuation[token]);
    }
}
