using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal class Quantifier : Token
    {
        public EMetacharacter Metacharacter { get; set; }
        //public List<Token> Tokens { get; set; } = new();

        public Quantifier(EMetacharacter metacharacter) => Metacharacter = metacharacter;

        public override string ToString()
        {
            return Metacharacter switch
            {
                EMetacharacter.None => "",
                EMetacharacter.Caret => "^",
                EMetacharacter.Dot => ".",
                EMetacharacter.SquareBracket => "[]",
                EMetacharacter.Dollar => "$",
                EMetacharacter.Bracket => "()",
                EMetacharacter.Nth => "\\n",
                EMetacharacter.Asterisk => "*",
                EMetacharacter.CurlyBracket => "{}",
                EMetacharacter.QuestionMark => "?",
                EMetacharacter.Plus => "+",
                EMetacharacter.VerticalBar => "|",
                //EMetacharacter.Group => "[]",
                EMetacharacter.NotGroup => "[^]",
                //EMetacharacter.Subexpression => "()",
                EMetacharacter.Set => "[()]",
                _ => throw new NotImplementedException()
            };
        }
    }
}
