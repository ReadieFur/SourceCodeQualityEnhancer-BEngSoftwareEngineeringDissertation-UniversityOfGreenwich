using System.Text.RegularExpressions;

namespace ReadieFur.SourceAnalyzer.Core.Config
{
    public class NamingConvention : ConfigBase
    {
        public string Pattern { get; set; }
        public ESeverity Severity { get; set; }

        //public Regex Regex => new Regex(Pattern);
    }
}
