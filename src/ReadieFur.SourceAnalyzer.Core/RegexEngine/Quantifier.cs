﻿using System;
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
        private object _testLock = new();
        private bool _isConforming = false;
        private int _conformCount = 0;
        private object _conformLock = new();

        public override Token? CanParse(ref string consumablePattern)
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

            lock (_testLock)
            {
                if (_isTesting)
                    return true;
                _isTesting = true;
            }

            if (_min is null || _max is null || Parent is null)
            {
                _isTesting = false;
                throw new InvalidOperationException();
            }

            //We need to keep a count of the index upon each iteration so that when an iteration fails we can reset the index to the last successful index for successor token checks.
            int lastSuccessfulIterationIndex = index;
            while (Parent.Test(input, ref index) && index < input.Length)
            {
                _testCount++;
                lastSuccessfulIterationIndex = index;
            }
            index = lastSuccessfulIterationIndex;

            lock (_testLock)
            {
                _isTesting = false;
            }

            bool result = _testCount >= _min && _testCount <= _max;
            _testCount = 0;
            return result;
        }

        public override bool Conform(string input, ref int index, ref string output)
        {
            lock (_conformLock)
            {
                if (_isConforming)
                    return true;
                _isConforming = true;
            }

            if (_min is null || _max is null || Parent is null)
            {
                _isConforming = false;
                throw new InvalidOperationException();
            }

            int lastSuccessfulIterationIndex = index;
            while (Parent.Conform(input, ref index, ref output) && index < input.Length && _conformCount < _max)
            {
                _conformCount++;
                lastSuccessfulIterationIndex = index;
            }
            index = lastSuccessfulIterationIndex;

            lock (_conformLock)
            {
                _isConforming = false;
            }

            bool result = _conformCount >= _min;
            _conformCount = 0;
            return result;
        }

        /// <summary>
        /// Check if a quantifier is present and if so, add it to the parent token.
        /// </summary>
        /// <param name="consumablePattern"></param>
        /// <param name="parent">Token dosen't need the ref keyword as objects are passed by reference by default.</param>
        /// <param name="endToken"></param>
        /// <returns>Returns the new end token or old end token if no quantifier was present.</returns>
        public static Token CheckForQuantifier(ref string consumablePattern, Token parent, Token endToken)
        {
            //Check if there are any quantifiers that could be applied to this group.
            Token? quantifier = new Quantifier().CanParse(ref consumablePattern);
            if (quantifier is not null)
            {
                parent.Children.Add(quantifier);
                quantifier.Parent = parent;
                quantifier.Previous = endToken;
                endToken.Next = quantifier;
                return quantifier.Parse(ref consumablePattern);
            }
            
            return endToken;
        }
    }
}
