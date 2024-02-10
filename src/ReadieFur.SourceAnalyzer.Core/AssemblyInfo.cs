using ReadieFur.SourceAnalyzer.Core;
using System.Reflection;
using System.Runtime.InteropServices;

//[assembly: AssemblyTitle($"{AssemblyInfo.ASSEMBLY_NAME}.VSIX")]
[assembly: AssemblyDescription(AssemblyInfo.DESCRIPTION)]
//[assembly: AssemblyConfiguration("")]
//[assembly: AssemblyCompany("")]
//[assembly: AssemblyProduct($"{AssemblyInfo.ASSEMBLY_NAME}.VSIX")]
[assembly: AssemblyCopyright(AssemblyInfo.AUTHOR)]
[assembly: AssemblyTrademark(AssemblyInfo.LICENSE)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: AssemblyVersion(AssemblyInfo.VERSION)]
//[assembly: AssemblyFileVersion(AssemblyInfo.VERSION)]


namespace ReadieFur.SourceAnalyzer.Core
{
    public static class AssemblyInfo
    {
        public const string PRODUCT_NAME = "Source Analyzer";
        public const string ASSEMBLY_NAME = "ReadieFur.SourceAnalyzer";
        public const string VERSION = "1.0.0";
        public const string AUTHOR = "ReadieFur";
        public const string LICENSE = "GPL3";
        public const string DESCRIPTION = "";
    }
}
