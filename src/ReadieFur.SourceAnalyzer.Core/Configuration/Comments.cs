namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public sealed class Comments : ConfigBase
    {
        public bool NewLine { get; set; } = false;
        public bool LeadingSpace { get; set; } = false;
        public bool TrailingFullStop { get; set; } = true;
        public bool CapitalizeFirstLetter { get; set; } = true;
        public double CommentDetectionSensitivity { get; set; } = 0.5d;
    }
}
