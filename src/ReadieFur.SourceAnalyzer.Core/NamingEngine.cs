using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

/*TODO: Split this shared project into it's own library that can be tied into various analyzers with this being the backing library.
 * With this provide a verification delegate and characters to split on.
 */
namespace ReadieFur.SourceAnalyzer.Core
{
    //Attempt to provide a new name for the symbol by checking against known naming conventions.
    //The aim is to "tear apart" the original name into a format that can be manipulated to match the provided naming schema.
    public class NamingEngine
    {
        #region Static
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<string> ConformToPatternAsync(string input, string pattern, CancellationToken? cancellationToken = null)
#pragma warning restore CS1998
        {
            NamingEngine engine = new(input, pattern, cancellationToken);
            await engine.Process();
            return engine.Result;
        }

        public static string ConformToPattern(string input, string pattern)
        {
            NamingEngine engine = new(input, pattern, null);
            engine.Process().Wait();
            return engine.Result;
        }
        #endregion

        #region Instance
        public string Result { get; private set; } = string.Empty;
        private readonly string _input;
        private readonly Regex _pattern;
        private readonly CancellationToken? _cancellationToken;
        private List<string> tokens = new();

        private NamingEngine(string input, string pattern, CancellationToken? cancellationToken = null)
        {
            _input = input;
            _pattern = new(pattern);
            _cancellationToken = cancellationToken;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task Process()
#pragma warning restore CS1998
        {
            Deconstruct();
            Reconstruct();
            Validate();
        }

        //TODO: Possibly create a dictionary of common words to split on if we cannot match reliably.
        private void Deconstruct()
        {
            //Iterate over the input string and split it into tokens based on the following:
            List<Predicate<char>> splitPredicates = new()
            {
                char.IsUpper,
                c => c == '_'
            };

            StringBuilder token = new();
            foreach (char @char in _input)
            {
                if (_cancellationToken?.IsCancellationRequested ?? false)
                    return;

                if (char.IsUpper(@char))
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                    token.Append(@char);
                }
                else if (@char == '_')
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }
                else
                {
                    token.Append(@char);
                }
            }

            tokens = tokens.FindAll(s => !string.IsNullOrEmpty(s));
        }

        private void Reconstruct()
        {
            //I believe in order for this to work I will need to create my own Regex engine so I can construct a string based on a pattern and input.
            throw new NotImplementedException();
        }

        private void Validate()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
