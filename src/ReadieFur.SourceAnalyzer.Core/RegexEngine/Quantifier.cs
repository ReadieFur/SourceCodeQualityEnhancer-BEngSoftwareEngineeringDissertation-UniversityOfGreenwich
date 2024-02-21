using System;
using System.Collections.Generic;

namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal class Quantifier : Token
    {
        private enum QuantifierType
        {
            ZeroOrOne,
            ZeroOrMore,
            OneOrMore,
            Range
        }

        private QuantifierType _type;
        private int? _min = null;
        private int? _max = null;
        private bool _isTesting = false; //It is ok to use a plain boolean here as this object should not be called asynchronously.
        private int _testCount = 0;

        public override Token CanParse(ref string consumablePattern)
        {
            if (consumablePattern.StartsWith("?"))
            {
                _type = QuantifierType.ZeroOrOne;
                _min = 0;
                _max = 1;
            }
            else if (consumablePattern.StartsWith("*"))
            {
                _type = QuantifierType.ZeroOrMore;
                _min = 0;
                _max = int.MaxValue;
            }
            else if (consumablePattern.StartsWith("+"))
            {
                _type = QuantifierType.OneOrMore;
                _min = 1;
                _max = int.MaxValue;
            }
            else if (consumablePattern.StartsWith("{"))
            {
                _type = QuantifierType.Range;
                //Ranges calculated in the Parse method.
            }
            else
            {
                return null;
            }

            return this;
        }

        public override Token Parse(ref string consumablePattern)
        {
            if (_type != QuantifierType.Range)
            {
                consumablePattern = consumablePattern.Substring(1);
                return this;
            }

            int consume = 0;
            List<char> buf = new();
            foreach (char c in consumablePattern)
            {
                //Make sure that only numbers, commas, and closing braces are present.
                if (!char.IsDigit(c) && c != ',' && c != '}')
                    throw new InvalidOperationException();

                consume++;

                if (char.IsDigit(c))
                {
                    buf.Add(c);
                }
                else if (c == ',')
                {
                    //If min is already set, then we have an invalid pattern as there can only be one comma.
                    if (_min is not null)
                        throw new InvalidOperationException();

                    //If there is no number before the comma, then the min is 0.
                    if (buf.Count == 0)
                    {
                        _min = 0;
                    }
                    else
                    {
                        _min = int.Parse(new string(buf.ToArray()));
                        buf.Clear();
                    }
                }
                else if (c == '}')
                {
                    //If min is not set, then we have an invalid pattern.
                    if (_min is null)
                        throw new InvalidOperationException();

                    //If there is no number after the comma, then the max is infinity.
                    if (buf.Count == 0)
                    {
                        _max = int.MaxValue;
                    }
                    else
                    {
                        _max = int.Parse(new string(buf.ToArray()));
                    }
                }
            }
            consumablePattern = consumablePattern.Substring(consume);

            //Quantifiers can't have any proceeding modifiers so just return this.
            return this;
        }

        public override bool Test(string input, ref int index)
        {
            //The way my quantifier works is it will call the Test method on it's parent.
            //Normally this would cause a stack overflow, so we implement a mutex to prevent that as the method will call itself through the parent.
            //So if we detect that we are already testing, the mutex will be locked so just return true,
            //as the parent will not reach this method if one of it's children fail (as the quantifier is always last).

            //TODO: I think I need to keep a count of the index upon each iteration so that when an iteration fails we can reset the index to the last successful index for successor token checks.
            if (_isTesting)
                return true;

            if (_min is null || _max is null || Parent is null)
                throw new InvalidOperationException();

            _isTesting = true;
            while (Parent.Test(input, ref index))
                _testCount++;
            _isTesting = false;

            bool result = _testCount >= _min && _testCount <= _max;
            _testCount = 0;
            return result;
        }
    }
}
