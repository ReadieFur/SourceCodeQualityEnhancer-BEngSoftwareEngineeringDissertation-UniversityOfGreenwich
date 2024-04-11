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
            /*CodeFixExpected =
                CodeFixInput =
                AnalyzerInput =
                noCustomUsingsText;*/
            #endregion

            #region Descriptors
            IEnumerable<KeyValuePair<NamingConvention, DiagnosticDescriptor>> namingDescriptors = Core.Analyzers.Helpers.GetNamingDescriptors();
            #endregion

            #region Interpretation
            string[] lines = noCustomUsingsText.Split(Environment.NewLine); //Use Environment.NewLine as this is to be a multi-platform solution so it needs support CLRF (\r\n windows) and LF (\n unix).

            KeyValuePair<NamingConvention, DiagnosticDescriptor>? currentDiagnostic = null;
            Dictionary<int, string> removeBuffer = new();
            Dictionary<int, string> addBuffer = new();

            void ProcessBuffer()
            {
                if (currentDiagnostic is null)
                    return;

                string removeString = string.Join(Environment.NewLine, removeBuffer.Values);
                string addString = string.Join(Environment.NewLine, addBuffer.Values);

                AnalyzerInput += removeString;
                CodeFixInput += $"{{|#{_codeFixDiagnostics.Count}:{addString}|}}";
                CodeFixExpected += addString;

                //Create diagnostic.
                DiagnosticDescriptor descriptor = new(
                    currentDiagnostic.Value.Value.Id,
                    currentDiagnostic.Value.Value.Title,
                    string.Format(currentDiagnostic.Value.Value.MessageFormat.ToString(), removeString, currentDiagnostic.Value.Key.Pattern),
                    currentDiagnostic.Value.Value.Category,
                    currentDiagnostic.Value.Value.DefaultSeverity,
                    currentDiagnostic.Value.Value.IsEnabledByDefault
                );
                DiagnosticResult analyzerDiagnosticResult = new(descriptor);

                //TODO: Get diagnostic bounds.
                void GetBounds(Dictionary<int, string> lines, out int startLine, out int startColumn, out int endLine, out int endColumn)
                {
                    startLine = lines.First().Key;
                    startColumn = lines.First().Value.IndexOf(lines.First().Value.First(c => !char.IsWhiteSpace(c)));
                    endLine = lines.Last().Key;
                    endColumn = lines.Last().Value.Length - 1;
                }
                GetBounds(removeBuffer, out int removeStartLine, out int removeStartColumn, out int removeEndLine, out int removeEndColumn);
                //GetBounds(addBuffer, out int addStartLine, out int addStartColumn, out int addEndLine, out int addEndColumn);
                
                analyzerDiagnosticResult = analyzerDiagnosticResult.WithLocation(removeStartLine, removeStartColumn);
                _analyzerDiagnostics.Add(analyzerDiagnosticResult);

                DiagnosticResult codeFixDiagnosticResult = NamingFixer.Diagnostic(descriptor).WithLocation(_codeFixDiagnostics.Count).WithArguments(addString, currentDiagnostic.Value.Key.Pattern!);
                _codeFixDiagnostics.Add(codeFixDiagnosticResult);

                //Clear the buffers.
                currentDiagnostic = null;
                removeBuffer.Clear();
                addBuffer.Clear();
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Length < 3 || !line.StartsWith("//"))
                {
                    if (i != 0)
                    {
                        AnalyzerInput += Environment.NewLine;
                        CodeFixInput += Environment.NewLine;
                        CodeFixExpected += Environment.NewLine;
                    }

                    ProcessBuffer();

                    AnalyzerInput += line;
                    CodeFixInput += line;
                    CodeFixExpected += line;

                    continue;
                }

                switch (line[2])
                {
                    case '#':
                        {
                            KeyValuePair<NamingConvention, DiagnosticDescriptor>? targetDiagnostic = namingDescriptors.FirstOrDefault(kvp => kvp.Value.Id == Core.Analyzers.Helpers.ANALYZER_ID_PREFIX + line.Substring(3));
                            if (targetDiagnostic is null)
                                Assert.Fail("No diagnostic found for ID: " + Core.Analyzers.Helpers.ANALYZER_ID_PREFIX + line.Substring(3));

                            ProcessBuffer();

                            currentDiagnostic = targetDiagnostic;
                        }
                        break;
                    case '-':
                    case '+':
                        {
                            if (currentDiagnostic is null)
                                Assert.Fail("No diagnostic ID provided");

                            string text = line.Substring(3);
                            if (string.IsNullOrEmpty(text))
                                continue;

                            if (line[2] == '-')
                                removeBuffer.Add(i, text);
                            else
                                addBuffer.Add(i, text);
                        }
                        break;
                }
            }
            #endregion
        }
    }
}
