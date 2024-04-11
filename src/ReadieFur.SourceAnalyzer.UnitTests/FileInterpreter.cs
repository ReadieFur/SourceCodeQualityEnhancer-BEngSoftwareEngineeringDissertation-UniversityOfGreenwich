using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using NamingFixer = ReadieFur.SourceAnalyzer.UnitTests.Verifiers.CSharpCodeFixVerifier<ReadieFur.SourceAnalyzer.Core.Analyzers.NamingAnalyzer, ReadieFur.SourceAnalyzer.Core.Analyzers.NamingFixProvider>;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    public class FileInterpreter
    {
        public readonly string SourceText;
        private List<DiagnosticResult> _analyzerDiagnostics = new();
        private List<DiagnosticResult> _codeFixDiagnostics = new();
        public string AnalyzerInput { get; private set; } = string.Empty;
        public string CodeFixInput { get; private set; } = string.Empty;
        public string CodeFixExpected { get; private set; } = string.Empty;
        public DiagnosticResult[] AnalyzerDiagnostics => _analyzerDiagnostics.ToArray();
        public DiagnosticResult[] CodeFixDiagnostics => _codeFixDiagnostics.ToArray();

        private FileInterpreter(string sourceText)
        {
            SourceText = sourceText;
        }

        public static async Task<FileInterpreter> Interpret(string fileName)
        {
            FileInterpreter fileInterpreter = new(await GetSourceFile(fileName));
            fileInterpreter.Process();
            return fileInterpreter;
        }

        private void Process()
        {
            #region Usings
            string noCustomUsingsText = SourceText;
            int usingsStart = SourceText.IndexOf("using", StringComparison.Ordinal);
            int usingsEnd = SourceText.LastIndexOf("using", StringComparison.Ordinal);
            string usings = SourceText.Substring(usingsStart, usingsEnd - usingsStart);
            foreach (string originalUsing in usings.Split('\n').Reverse())
                if (originalUsing.StartsWith("using ReadieFur"))
                    noCustomUsingsText = noCustomUsingsText.Replace(originalUsing, string.Empty);
            CodeFixExpected =
                CodeFixInput =
                AnalyzerInput =
                noCustomUsingsText;
            #endregion

            #region Descriptors
            IEnumerable<KeyValuePair<NamingConvention, DiagnosticDescriptor>> namingDescriptors = Core.Analyzers.Helpers.GetNamingDescriptors();
            #endregion

            #region Interpretation
            string[] lines = noCustomUsingsText.Split(Environment.NewLine); //Use Environment.NewLine as this is to be a multi-platform solution so it needs support CLRF (\r\n windows) and LF (\n unix).
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                //Check if the line indicates the start of a diagnostic section.
                if (line.Length < 3 || !line.StartsWith("//#"))
                    continue;

                //Get the diagnostic.
                KeyValuePair<NamingConvention, DiagnosticDescriptor>? targetDiagnostic = namingDescriptors.FirstOrDefault(kvp => kvp.Value.Id == Core.Analyzers.Helpers.ANALYZER_ID_PREFIX + line.Substring(3));
                if (targetDiagnostic is null)
                    throw new Exception("No diagnostic found for ID: " + Core.Analyzers.Helpers.ANALYZER_ID_PREFIX + line.Substring(3));

                int startLine = i;
                int startColumn = line.IndexOf(line.First(c => !char.IsWhiteSpace(c)));
                List<string> removeLines = new();
                List<string> addLines = new();
                int endOffset = 0;

                //Get input data.
                bool onRemoveToken = true;
                for (; i < lines.Length; i++)
                {
                    line = lines[i];
                    if (line.Length < 3)
                    {
                        i--; //Decrement for the external i++.
                        break;
                    }

                    bool isRemoveToken = line[2] == '-';
                    if (!onRemoveToken && isRemoveToken)
                        throw new InvalidOperationException("All addition tokens must succeed subtract tokens");

                    string subLine = line.Substring(3);

                    if (isRemoveToken)
                        removeLines.Add(subLine);
                    else
                        addLines.Add(subLine);

                    endOffset += subLine.Length;
                }
            }
            #endregion
        }
    }
}
