namespace ReadieFur.SourceAnalyzer.Core
{
    //I have switched to using a shared project for the core library. Normally I would use a class library, howver due to various complications with the VSIX project, I have decided to use a shared project instead.
    internal static class AssemblyInfo
    {
        public const string PRODUCT_NAME = "Source Analyzer";
        public const string ASSEMBLY_NAME = "ReadieFur.SourceAnalyzer";
        public const string VERSION = "1.0.0";
        public const string AUTHOR = "ReadieFur";
        public const string LICENSE = "GPL3";
        public const string DESCRIPTION = "";
    }
}
