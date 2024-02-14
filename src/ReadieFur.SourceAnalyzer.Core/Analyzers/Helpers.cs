using Microsoft.CodeAnalysis;
using ReadieFur.SourceAnalyzer.Core.Config;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using static ReadieFur.SourceAnalyzer.Core.Config.ConfigMaster;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    internal static class Helpers
    {
        public const string ANALYZER_ID_PREFIX = "SA";

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

        public static IEnumerable<KeyValuePair<NamingConvention, DiagnosticDescriptor>> GetNamingDescriptors()
        {
            //Using reflection here is ok as it is a one-time thing, I am only using it to reduce the amount of code I have to manually write here (ideally I'd have something auto-generate static code here).
            foreach (PropertyInfo? prop in typeof(Naming).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop is null
                    || prop.PropertyType != typeof(NamingConvention)
                    || !TryGetAnalyzerID(prop.Name, out string id, out ENamingAnalyzer enumValue)
                    || prop.GetValue(Configuration.Naming) is not NamingConvention value
                    || string.IsNullOrEmpty(value.Pattern))
                    continue;

                //Make sure the regex format is valid.
                try { new Regex(value.Pattern); }
                catch { continue; }

                DiagnosticSeverity severity;
                switch (value.Severity)
                {
                    case ESeverity.None:
                        severity = DiagnosticSeverity.Hidden;
                        break;
                    case ESeverity.Info:
                        severity = DiagnosticSeverity.Info;
                        break;
                    case ESeverity.Warning:
                        severity = DiagnosticSeverity.Warning;
                        break;
                    case ESeverity.Error:
                        severity = DiagnosticSeverity.Error;
                        break;
                    default:
                        //This shouldn't be reached.
                        throw new InvalidOperationException();
                }

                yield return new(value, new(
                    id: id,
                    title: $"{prop.Name} does not match the provided naming schema.",
                    messageFormat: "'{0}' does not match the regular expression '{1}'",
                    category: "Naming",
                    defaultSeverity: severity,
                    isEnabledByDefault: value.Enabled
                ));
            }
        }
    }
}
