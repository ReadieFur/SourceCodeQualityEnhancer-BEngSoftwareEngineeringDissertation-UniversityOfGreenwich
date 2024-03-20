using Microsoft.CodeAnalysis;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    internal static class Helpers
    {
        //https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/choosing-diagnostic-ids?source=recommendations
        public const string ANALYZER_ID_PREFIX = "RFSA"; //ReadieFur Source Analyzer

        public static bool TryGetAnalyzerID<TEnum>(string propertyName, out string id, out TEnum enumValue) where TEnum : struct, IConvertible
        {
            id = string.Empty;
            enumValue = default;

            //https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum
            if (!typeof(TEnum).IsEnum || !Enum.TryParse(propertyName, out enumValue))
                return false;

            //https://stackoverflow.com/questions/16960555/how-do-i-cast-a-generic-enum-to-int
            id = ANALYZER_ID_PREFIX + ((int)(object)enumValue).ToString().PadLeft(4, '0');
            return true;
        }

        public static bool TryGetAnalyzerType<TEnum>(string analyzerID, out TEnum enumValue) where TEnum : struct, IConvertible
        {
            enumValue = default;

            if (!typeof(TEnum).IsEnum)
                return false;

            //https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum
            if (analyzerID.Length < ANALYZER_ID_PREFIX.Length + 4
                || !analyzerID.StartsWith(ANALYZER_ID_PREFIX)
                || !int.TryParse(analyzerID.Substring(ANALYZER_ID_PREFIX.Length), out int value)
                || !Enum.IsDefined(typeof(TEnum), value))
                return false;

            enumValue = (TEnum)(object)value;
            return true;
        }

        public static IEnumerable<KeyValuePair<NamingConvention, DiagnosticDescriptor>> GetNamingDescriptors()
        {
            //Using reflection here is ok as it is a one-time thing, I am only using it to reduce the amount of code I have to manually write here (ideally I'd have something auto-generate static code here).
            foreach (PropertyInfo? prop in typeof(Naming).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop is null
                    || prop.PropertyType != typeof(NamingConvention)
                    || !TryGetAnalyzerID(prop.Name, out string id, out ENamingAnalyzer enumValue)
                    || prop.GetValue(ConfigManager.Configuration.Naming) is not NamingConvention value
                    || string.IsNullOrEmpty(value.Pattern))
                    continue;

                //Make sure the regex format is valid.
                try { new Regex(value.Pattern); }
                catch { continue; }

                yield return new(value, new(
                    id: id,
                    title: $"{prop.Name} does not match the provided naming schema.",
                    messageFormat: "'{0}' does not match the regular expression '{1}'",
                    category: "Naming",
                    defaultSeverity: value.Severity.ToDiagnosticSeverity(),
                    isEnabledByDefault: value.IsEnabled
                ));
            }
        }

        public static DiagnosticSeverity ToDiagnosticSeverity(this ESeverity severity)
        {
            return severity switch
            {
                ESeverity.None => DiagnosticSeverity.Hidden,
                ESeverity.Info => DiagnosticSeverity.Info,
                ESeverity.Warning => DiagnosticSeverity.Warning,
                ESeverity.Error => DiagnosticSeverity.Error,
                _ => throw new InvalidOperationException()
            };
        }
    }
}
