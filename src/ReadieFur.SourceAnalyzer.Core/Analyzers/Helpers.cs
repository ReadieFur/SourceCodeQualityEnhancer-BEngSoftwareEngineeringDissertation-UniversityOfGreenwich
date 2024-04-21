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

        public static bool TryGetAnalyzerType(string analyzerID, out EAnalyzerID enumValue)
        {
            enumValue = default;

            //https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum
            if (analyzerID.Length < ANALYZER_ID_PREFIX.Length + 4
                || !analyzerID.StartsWith(ANALYZER_ID_PREFIX)
                || !int.TryParse(analyzerID.Substring(ANALYZER_ID_PREFIX.Length), out int value)
                || !Enum.IsDefined(typeof(EAnalyzerID), value))
                return false;

            enumValue = (EAnalyzerID)(object)value;
            return true;
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

        public static DiagnosticSeverity GetDiagnosticSeverity(ESeverity? severity)
        {
            if (!severity.HasValue)
                return DiagnosticSeverity.Hidden;
            else
                return severity.Value.ToDiagnosticSeverity();
        }

        public static string ToTag(this EAnalyzerID self)
        {
            return ANALYZER_ID_PREFIX + ((int)self).ToString().PadLeft(4, '0');
        }
    }
}
