using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Text.RegularExpressions;

namespace ReadieFur.SourceAnalyzer.UnitTests
{
    internal class FileInterpreter
    {
        public readonly Type SourceType;
        public readonly string SourceText;
        private List<DiagnosticResult> _analyzerDiagnostics = new();
        //private List<DiagnosticResult> _codeFixDiagnostics = new();
        public string AnalyzerInput { get; private set; } = string.Empty;
        public string CodeFixInput { get; private set; } = string.Empty;
        public string CodeFixExpected { get; private set; } = string.Empty;
        public DiagnosticResult[] AnalyzerDiagnostics => _analyzerDiagnostics.ToArray();
        //public IReadOnlyList<DiagnosticResult> CodeFixDiagnostics => _codeFixDiagnostics;

        private FileInterpreter(Type sourceType, string sourceText)
        {
            SourceType = sourceType;
            SourceText = sourceText;
        }

        public static async Task<FileInterpreter> Interpret(Type sourceType)
        {
            FileInterpreter fileInterpreter = new(sourceType, await GetSourceFile(sourceType));
            fileInterpreter.Process();
            return fileInterpreter;
        }

        private void Process()
        {
            #region Usings
            string noCustomUsings = SourceText;
            int usingsStart = SourceText.IndexOf("using", StringComparison.Ordinal);
            int usingsEnd = SourceText.LastIndexOf("using", StringComparison.Ordinal);
            string usings = SourceText.Substring(usingsStart, usingsEnd - usingsStart);
            foreach (string originalUsing in usings.Split('\n').Reverse())
                if (originalUsing.StartsWith("using ReadieFur"))
                    noCustomUsings = noCustomUsings.Replace(originalUsing, string.Empty);
            CodeFixExpected =
                CodeFixInput =
                AnalyzerInput =
                noCustomUsings;
            #endregion

            #region Descriptors
            IEnumerable<KeyValuePair<NamingConvention, DiagnosticDescriptor>> namingDescriptors = Core.Analyzers.Helpers.GetNamingDescriptors();
            #endregion

            #region Interpretation
            //Search over the source text.
            Regex regex = new(@"\/\*([^*]+)\*\/([^*]+)\/\*([^*]+)\*\/", RegexOptions.Multiline);
            Match match;
            int offset = 0;
            string regexSource = SourceText;
            while ((match = regex.Match(regexSource)).Success) //The overload "startat" didn't seem to be working for me so I am manually tracking the offset.
            {
                string diagnosticId = match.Groups[1].Value;
                string input = match.Groups[2].Value;
                string expected = match.Groups[3].Value;
                int start = match.Index + offset;
                int length = match.Length;
                regexSource = regexSource.Substring(match.Index + match.Length);
                offset += match.Index + match.Length;

                #region Modifications
                //For each of the various interpretations, replace the input with the expected value.
                AnalyzerInput = AnalyzerInput.Remove(start, length).Insert(start, input);
                //https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix
                string codeFixToken = $"{{|{diagnosticId}:{input}|}}";
                CodeFixInput = CodeFixInput.Remove(start, length).Insert(start, codeFixToken);
                CodeFixExpected = CodeFixExpected.Remove(start, length).Insert(start, expected);
                #endregion

                #region Diagnostics
                //Find the diagnostic descriptor that matches the diagnostic ID.
                KeyValuePair<NamingConvention, DiagnosticDescriptor>? namingDescriptor = namingDescriptors.FirstOrDefault(kvp => kvp.Value.Id == diagnosticId);
                if (namingDescriptor is null)
                    throw new InvalidOperationException($"Could not find diagnostic descriptor for '{diagnosticId}'.");
                DiagnosticDescriptor descriptor = new(
                    namingDescriptor.Value.Value.Id,
                    namingDescriptor.Value.Value.Title,
                    string.Format(namingDescriptor.Value.Value.MessageFormat.ToString(), input, namingDescriptor.Value.Key.Pattern),
                    namingDescriptor.Value.Value.Category,
                    namingDescriptor.Value.Value.DefaultSeverity,
                    namingDescriptor.Value.Value.IsEnabledByDefault
                );
                DiagnosticResult diagnosticResult = new(descriptor);

                //Get the bounds of the diagnostic.
                string startPart = AnalyzerInput.Substring(0, start);
                string endPart = AnalyzerInput.Substring(0, start + input.Length);
                int startLine = startPart.Count(c => c == '\n') + 1; //The analyzer uses 1-based indexing.
                int startColumn = startPart.Split('\n').Last().Length + 1;
                /*int analyzerEndLine = endPart.Count(c => c == '\n');
                int analyzerEndColumn = endPart.Split('\n').Last().Length;*/
                /*int codeFixEndLine = SourceText.Substring(start, codeFixToken.Length).Count(c => c == '\n');
                int codeFixEndColumn = SourceText.Substring(start, codeFixToken.Length).LastIndexOf('\n');*/

                //Update the diagnostic result with the bounds.
                diagnosticResult = diagnosticResult.WithLocation(startLine, startColumn);
                _analyzerDiagnostics.Add(diagnosticResult);
                #endregion
            }
            #endregion
        }
    }
}
