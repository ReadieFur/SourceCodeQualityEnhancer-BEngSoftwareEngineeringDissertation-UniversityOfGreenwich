using System;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    [Flags]
    internal enum EMetacharacter : int
    {
        #region POSIX Basic
        None                                = 1 << 0,
        Caret                               = 1 << 1,                       //^
        Dot                                 = 1 << 2,                       //.
        SquareBracket                       = 1 << 3,                       //[]
        Dollar                              = 1 << 4,                       //$
        Bracket                             = 1 << 5,                       //()
        Nth                                 = 1 << 6,                       //\n
        Asterisk                            = 1 << 7,                       //*
        CurlyBracket                        = 1 << 8,                       //{}
        #endregion

        #region POSIX Extended
        QuestionMark                        = 1 << 9,                       //?
        Plus                                = 1 << 10,                      //+
        VerticalBar                         = 1 << 11,                      //|
        #endregion

        #region Types
        Group                               = SquareBracket,                //[]
        NotGroup                            = SquareBracket | Caret,        //[^]
        Subexpression                       = Bracket,                      //()
        Set                                 = SquareBracket | Bracket,      //[()]
        #endregion
    }
}
