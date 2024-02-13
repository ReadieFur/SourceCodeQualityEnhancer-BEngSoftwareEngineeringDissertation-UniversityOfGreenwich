using System;

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
    }
}
