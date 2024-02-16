using System.Text.RegularExpressions;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    //These templates have been split into "groups" so that the analyzers can pick apart the string.
    internal class RegexTemplates
    {
        internal static readonly Regex PascalCase = new("^[A-Z][a-z]+(?:[A-Z][a-z]+)*$");
        internal static readonly Regex CamelCase = new("^[a-z]+(?:[A-Z][a-z]+)*$");
        internal static readonly Regex SpecialCharacters = new(@"[^a-zA-Z0-9\s]");
        internal static readonly Regex IllegalCharacters = new(@"[^a-zA-Z0-9_@\s]");
    }
}
