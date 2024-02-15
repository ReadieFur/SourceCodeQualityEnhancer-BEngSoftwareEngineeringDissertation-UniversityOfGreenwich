using System.Text.RegularExpressions;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    internal class RegexTemplates
    {
        internal static readonly Regex PascalCase = new("^[A-Z][a-z]+(?:[A-Z][a-z]+)*$");
        internal static readonly Regex CamelCase = new("^[a-z]+(?:[A-Z][a-z]+)*$");
        internal static readonly Regex SpecialCharacters = new(@"[^a-zA-Z0-9\s]");
    }
}
