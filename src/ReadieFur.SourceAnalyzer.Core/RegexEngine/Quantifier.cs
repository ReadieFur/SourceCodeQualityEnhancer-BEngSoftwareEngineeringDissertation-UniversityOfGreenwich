using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Reflection;

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

        /// <summary>
        /// Check if a quantifier is present and if so, add it to the parent token.
        /// </summary>
        /// <param name="consumablePattern"></param>
        /// <param name="relatedToken">Token dosen't need the ref keyword as objects are passed by reference by default.</param>
        /// <param name="endToken"></param>
        /// <returns>Returns the new end token or old end token if no quantifier was present.</returns>
        public static Token CheckForQuantifier(ref string consumablePattern, Token relatedToken, Token endToken)
        {
            //Check if there are any quantifiers that could be applied to this group.
            Token? quantifier = new Quantifier().CanParse(ref consumablePattern);
            if (quantifier is not null)
            {
                //Take the parent's place as the quantifier needs to come first (for later processing reasons).
                quantifier.Next = relatedToken;
                quantifier.Previous = relatedToken.Previous;
                quantifier.Parent = relatedToken.Parent;
                quantifier.Children.Add(relatedToken);

                //Occurs if this quantifier isn't top level.
                if (relatedToken.Parent is not null)
                {
                    //Update the parent's parent reference to point to the quantifier.
                    relatedToken.Parent.Children.Remove(relatedToken);
                    relatedToken.Parent.Children.Add(quantifier);
                    relatedToken.Parent.Next = quantifier;
                }

                //This must come after all parent updates.
                relatedToken.Previous = quantifier;
                relatedToken.Parent = quantifier;

                endToken = quantifier.Parse(ref consumablePattern);

                quantifier.Pattern = relatedToken.Pattern + quantifier.Pattern;

                return endToken;
            }

            return endToken;
        }

        private QuantifierType _type;
        private int? _min = null;
        private int? _max = null;

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
            string startingPattern = consumablePattern;

            if (_type != QuantifierType.Range)
            {
                consumablePattern = consumablePattern.Substring(1);

                Pattern = startingPattern.Substring(0, startingPattern.Length - consumablePattern.Length);

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

            Pattern = startingPattern.Substring(0, startingPattern.Length - consumablePattern.Length);

            //Quantifiers can't have any proceeding modifiers so just return this.
            return this;
        }

        public override bool Test(string input, ref int index)
        {
            //The way my quantifier works is it will call the Test method on it's parent.
            //Normally this would cause a stack overflow, so we implement a mutex to prevent that as the method will call itself through the parent.
            //So if we detect that we are already testing, the mutex will be locked so just return true,
            //as the parent will not reach this method if one of it's children fail (as the quantifier is always last).

            //A quantifier MUST have one and only one child.
            if (_min is null || _max is null || Children.Count != 1)
                throw new InvalidOperationException();

            int testCount = 0;

            //We need to keep a count of the index upon each iteration so that when an iteration fails we can reset the index to the last successful index for successor token checks.
            int lastSuccessfulIterationIndex = index;
            while (index < input.Length && testCount < _max && Children[0].Test(input, ref index))
            {
                testCount++;
                lastSuccessfulIterationIndex = index;
            }
            index = lastSuccessfulIterationIndex;

            bool result = testCount >= _min && testCount <= _max;
            return result;
        }

        public override bool Conform(ConformParameters parameters)
        {
            if (_min is null || _max is null || Children.Count != 1)
                throw new InvalidOperationException();

            ConformQuantifierOptions options = GetConformQuantifierOptions(parameters);

            int conformCount = 0;

            //TODO: try to make the greedy quantifiers not greedy (if even possible, perhaps pass an options object that allows for breaking on certain patterns for manual interpretation).
            int lastSuccessfulIterationIndex = parameters.Index;
            bool isGreedy = _max == int.MaxValue;
            try
            {
                //The order in which these checks are done IS important as we are dealing with references as well as the conform method not supposed to being able to run when the input has been saturated.
                while (conformCount < _max && parameters.Index < parameters.Input.Length && Children[0].Conform(parameters))
                {
                    conformCount++;
                    lastSuccessfulIterationIndex = parameters.Index;

                    //If this is a greedy quantifier then we need to check against the provided options...
                    if (!isGreedy)
                        continue;

                    char nextChar = parameters.Input[parameters.Index];

                    //We need to keep track of when we split on case change so that we don't split on the same case change twice as this can occur for nested quantifiers.
                    if (options.GreedyQuantifiersSplitOnCaseChange!.Value && parameters.LastGreedyQuantifiersSplitOnCaseChange != parameters.Index)
                    {
                        char previousChar = parameters.Input[parameters.Index - 1];

                        if (char.IsLetter(previousChar)
                            && char.IsLetter(nextChar)
                            && char.IsUpper(previousChar) != char.IsUpper(nextChar))
                        {
                            parameters.LastGreedyQuantifiersSplitOnCaseChange = parameters.Index;
                            break;
                        }
                    }

                    if (options.GreedyQuantifiersSplitOnAlphanumericChange!.Value)
                    {
                        char previousChar = parameters.Input[parameters.Index - 1];

                        if (char.IsLetterOrDigit(previousChar) != char.IsLetterOrDigit(nextChar))
                            break;
                    }

                    bool encounteredDelimiter = false;
                    foreach (char delimiter in options.GreedyQuantifierDelimiters!)
                    {
                        if (nextChar != delimiter)
                            continue;

                        //As the character is a delimiter, we must consume it.
                        lastSuccessfulIterationIndex = ++parameters.Index;
                        encounteredDelimiter = true;
                        break;
                    }
                    if (encounteredDelimiter)
                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                //Do nothing on this type of exception.
                //Can coccur when the conform tree reaches the end of the input string.
                //This dosen't occur for the test method as the test method has a known pattern to follow whereas the input pattern of the conform method is unknown.
            }
            parameters.Index = lastSuccessfulIterationIndex;

            bool result = conformCount >= _min;
            return result;
        }

        private ConformQuantifierOptions GetConformQuantifierOptions(ConformParameters parameters)
        {
            ConformQuantifierOptions options = parameters.Options.GlobalQuantifierOptions;

            //Determine the depth of this quantifier within the tree.
            int depth = -1; //Start at -1 as the regex engine always wraps the pattern in a group.
            Token? parent = Parent;
            while (parent is not null)
            {
                depth++;
                parent = parent.Parent;
            }

            if (parameters.Options.QuanitifierOptions.ContainsKey(depth))
            {
                //Combine the global options with the local options with the local options overriding the global options.
                ConformQuantifierOptions localOptions = parameters.Options.QuanitifierOptions[depth];

                /*//https://stackoverflow.com/questions/6280506/is-there-a-way-to-set-properties-on-struct-instances-using-reflection
                object newOptions = options;*/

                foreach (PropertyInfo property in typeof(ConformQuantifierOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    //If the field is not the default value then override it. TODO: I might need to change this to use nullables so I can check for null instead of default.
                    object? localOption = property.GetValue(localOptions);

                    //https://stackoverflow.com/questions/2490244/default-value-of-a-type-at-runtime
                    object? defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                    if (localOption != defaultValue)
                    //if (localOption != property.GetValue(options))
                    {
                        //This dosen't work on structs as they are value types and are copied by value when reassigned, see the link outside of this loop for a solution.
                        property.SetValue(options, property.GetValue(localOptions), null);
                        //property.SetValue(newOptions, property.GetValue(localOptions), null);
                    }
                }

                /*options = (ConformQuantifierOptions)newOptions;*/
            }

            return options;
        }
    }
}
