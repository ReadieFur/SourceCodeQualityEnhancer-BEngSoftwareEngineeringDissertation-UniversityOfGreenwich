namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    internal class NamingAnalyzerAttribute : Attribute
    {
        public string ValidationFormat { get; set; }

        public NamingAnalyzerAttribute(string validationFormat)
        {
            ValidationFormat = validationFormat;
        }
    }
}
