using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    /*References:
     * https://kean.blog/post/lets-build-regex
     * https://web.mit.edu/6.005/www/fa15/classes/17-regex-grammars/
     * https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_expressions/Cheatsheet
     * https://en.wikipedia.org/wiki/Regular_expression
     * https://github.com/firasdib/Regex101
     */
    public class Regex
    {
        public readonly string Pattern;

        private readonly Token _root;
        private Quantifier? _parent;
        private Token _current;

        public Regex(string pattern)
        {
            Pattern = pattern;

            _parent = null;
            _root = new Quantifier(EMetacharacter.Subexpression);
            _current = _root;

            ParsePattern();
        }

        private void ParsePattern()
        {
            for (int i = 0; i < Pattern.Length; i++)
            {
                char c = Pattern[i];

                switch (c)
                {
                    #region Group types
                    case '[':
                        char? nextChar = CharAt(i + 1);
                        switch (nextChar)
                        {
                            case '^':
                                OpenQuantifier(EMetacharacter.NotGroup);
                                i++;
                                break;
                            default:
                                OpenQuantifier(EMetacharacter.Group);
                                break;
                        }
                        break;
                    case '(':
                        //If the parent is a group then the ( should be interpreted as a set, otherwise it should be a subexpression.
                        OpenQuantifier(_parent is Quantifier && _parent.Metacharacter.HasFlag(EMetacharacter.Group) ? EMetacharacter.Set : EMetacharacter.Subexpression);
                        break;
                    case '{':
                        OpenQuantifier(EMetacharacter.CurlyBracket);
                        break;
                    case ']':
                        CloseQuantifier(EMetacharacter.Group);
                        break;
                    case ')':
                        CloseQuantifier(EMetacharacter.Subexpression);
                        break;
                    case '}':
                        CloseQuantifier(EMetacharacter.CurlyBracket);
                        break;
                    #endregion
                    #region Metacharacters
                    case '^':
                        break;
                    case '.':
                        break;
                    case '$':
                        break;
                    case '\\':
                        break;
                    case '*':
                        break;
                    case ',':
                        break;
                    case '?':
                        break;
                    case '+':
                        break;
                    case '|':
                        break;
                    #endregion
                    #region Atom
                    default:
                        Atom atom = new(c)
                        {
                            Parent = _parent,
                            Previous = _current
                        };
                        _current.Next = atom;
                        _current = atom;
                        break;
                        #endregion
                }
            }
        }

        private void OpenQuantifier(EMetacharacter metacharacter)
        {
            //Create the new token and set it's parent and previous to the current token.
            Quantifier nextToken = new(metacharacter)
            {
                Parent = _parent,
                Previous = _current
            };
            _current.Next = nextToken;
            //The new parent will be the quantifier we just created.
            _parent = nextToken;
            _current = nextToken;
        }

        private void CloseQuantifier(EMetacharacter metacharacter)
        {
            //Ensure that the quantifier we are closing matches the current parent metacharacter, if it doesn't then the pattern syntax is incorrect.
            if (_parent is null)
                throw new InvalidOperationException("No parent token to close.");

            //I am not quite sure but may need to check this against the exact metacharacter (adds complexity).
            //if (_parent.Metacharacter != metacharacter)
            if (!_parent.Metacharacter.HasFlag(metacharacter))
                throw new InvalidOperationException("Mismatched parent token.");

            //Create a new token to close the quantifier.
            Quantifier closingQuantifier = new(metacharacter)
            {
                Parent = _parent.Parent,
                Previous = _current
            };
            _current.Next = closingQuantifier;
            _current = closingQuantifier;

            //The new parent will be the parent of the quantifier we just closed as we have navigated up the tree.
            _parent = closingQuantifier.Parent as Quantifier;
        }

        private char? CharAt(int index)
        {
            if (index < 0 || index >= Pattern.Length)
                return null;
            return Pattern[index];
        }

        //For debugging purposes.
        public override string ToString()
        {
            StringBuilder sb = new();
            Token current = _root;
            while (current != null)
            {
                sb.Append(current);
                current = current.Next;
            }
            return sb.ToString();
        }

        public void Test(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                //Compare to the nodes in the tree.
            }
        }
    }
}
