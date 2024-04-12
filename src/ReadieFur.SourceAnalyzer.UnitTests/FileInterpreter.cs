using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Text.RegularExpressions;

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

        public delegate DiagnosticDescriptor AnalyzerDiagnosticCallback(string diagnosticID, string input);
        public delegate DiagnosticResult CodeFixDiagnosticCallback(DiagnosticDescriptor diagnosticDescriptor, string input);

        private FileInterpreter(string sourceText)
        {
            SourceText = sourceText;
        }

        public static async Task<FileInterpreter> Interpret(string fileName, AnalyzerDiagnosticCallback getDiagnostic, CodeFixDiagnosticCallback getCodeFixDiagnostic)
        {
            FileInterpreter fileInterpreter = new(await GetSourceFile(fileName));
            fileInterpreter.Process(getDiagnostic, getCodeFixDiagnostic);
            return fileInterpreter;
        }

        private void Process(AnalyzerDiagnosticCallback getAnalyzerDiagnostic, CodeFixDiagnosticCallback getCodeFixDiagnostic)
        {
            #region Usings
            string noCustomUsings = SourceText;
            int usingsStart = SourceText.IndexOf("using", StringComparison.Ordinal);
            int usingsEnd = SourceText.LastIndexOf("using", StringComparison.Ordinal);
            string usings = SourceText.Substring(usingsStart, usingsEnd - usingsStart);
            foreach (string originalUsing in usings.Split(Environment.NewLine).Reverse())
                if (!string.IsNullOrEmpty(originalUsing) && !originalUsing.StartsWith("using System"))
                    noCustomUsings = noCustomUsings.Replace(originalUsing, string.Empty);
            CodeFixExpected =
                CodeFixInput =
                AnalyzerInput =
                noCustomUsings;
            #endregion

            List<Diagnostic> diagnostics = new();

            #region Interpretation
            /* Outer regex breakdown:
             * Checks for comments on a new line (or the first line) suffixed with the predefined tokens +-#.
             * Captures the token and any text suffixing it.
             */
            Regex outerRegex = new(@"(?:^|\n)\/\/(#)(.*)", RegexOptions.Multiline);

            /* Inner regex breakdown:
             * Search for the above and select only instances where the next line does not match the pattern "//[+-]".
             * We don't capture any groups as I have no need to here.
             */
            Regex innerRegex = new(@"(?:^|\n)\/\/[+-].*?(?=\n(?!(\/\/[+-])))", RegexOptions.Multiline);

            Match outerMatch;
            int analyzerInputOffset = 0;
            int codeFixInputOffset = 0;
            int codeFixExpectedOffset = 0;
            string regexSource = noCustomUsings.ToString(); //Create a destructible copy of the source text to work on.
            int count = 0;
            while ((outerMatch = outerRegex.Match(regexSource)).Success) //The overload "startat" didn't seem to be working for me so I am manually tracking the offset.
            {
                //TODO: I've realised this dosent check that the block is preceeding the diagnostic line.
                //Each block must have -+.
                Match innerMatch = innerRegex.Match(regexSource);
                if (!innerMatch.Success)
                    throw new InvalidOperationException("Invalid block detected.");

                //Calculate the source bounds.
                int blockStart = outerMatch.Index + 1;
                int blockEnd = innerMatch.Index + innerMatch.Length;
                int blockLength = blockEnd - blockStart;

                //Remove the block from the input sources.
                analyzerInputOffset += blockStart;
                codeFixInputOffset += blockStart;
                codeFixExpectedOffset += blockStart;
                AnalyzerInput = AnalyzerInput.Remove(analyzerInputOffset, blockLength);
                CodeFixInput = CodeFixInput.Remove(codeFixInputOffset, blockLength);
                CodeFixExpected = CodeFixExpected.Remove(codeFixExpectedOffset, blockLength);

                //Generate the text to put in place of the source block for each input type.
                string[] blockLines = regexSource.Substring(blockStart, blockLength).Split(Environment.NewLine);
                IEnumerable<string> removeLines = blockLines.Where(s => s[2] == '-').Select(s => s.Substring(3).TrimEnd(Environment.NewLine.ToCharArray()));
                IEnumerable<string> addLines = blockLines.Where(s => s[2] == '+').Select(s => s.Substring(3).TrimEnd(Environment.NewLine.ToCharArray()));
                string analyzerInput = string.Join(Environment.NewLine, removeLines);
                string codeFixExpected = string.Join(Environment.NewLine, addLines);
                string codeFixInput; //Assigned below.

                //Check if there is a sub-input for the analyzer text (denoted by a string sourrounded by double-single quotation marks ''TEXT'').
                Match analyzerSubInputMatch = Regex.Match(analyzerInput, @"'{2}(.*)'{2}", RegexOptions.Multiline);
                string analyzerSubInput;
                if (analyzerSubInputMatch.Success)
                {
                    analyzerSubInput = analyzerSubInputMatch.Groups[1].Value;
                    codeFixInput = analyzerInput.Substring(0, analyzerSubInputMatch.Index) +
                        $"{{|#{count}:{analyzerSubInputMatch.Groups[1].Value}|}}" +
                        analyzerInput.Substring(analyzerSubInputMatch.Index + analyzerSubInputMatch.Length);
                    analyzerInput = analyzerInput.Remove(analyzerSubInputMatch.Index, analyzerSubInputMatch.Length)
                        .Insert(analyzerSubInputMatch.Index, analyzerSubInputMatch.Groups[1].Value);
                }
                else
                {
                    analyzerSubInput = analyzerInput;
                    codeFixInput = $"{{|#{count}:{analyzerInput}|}}";
                }


                //Update the source texts.
                AnalyzerInput = AnalyzerInput.Insert(analyzerInputOffset, analyzerInput);
                CodeFixInput = CodeFixInput.Insert(codeFixInputOffset, codeFixInput);
                CodeFixExpected = CodeFixExpected.Insert(codeFixExpectedOffset, codeFixExpected);

                //Generate the diagnostics.
                string targetDiagnostic = Core.Analyzers.Helpers.ANALYZER_ID_PREFIX + outerMatch.Groups[2].Value.TrimEnd(Environment.NewLine.ToCharArray());
                if (getAnalyzerDiagnostic(targetDiagnostic, analyzerSubInput) is not DiagnosticDescriptor descriptor)
                    throw new InvalidOperationException($"Could not find diagnostic descriptor for '{targetDiagnostic}'.");

                //Analyzer diagnostic.
                int analyzerSubInputOffset = analyzerInput.IndexOf(analyzerSubInput);
                string analyzerStartPart = AnalyzerInput.Substring(0, analyzerInputOffset + analyzerSubInputOffset);
                string analyzerEndPart = AnalyzerInput.Substring(0, analyzerInputOffset + analyzerSubInputOffset + analyzerSubInput.Length);
                int analyzerStartLine = analyzerStartPart.Count(c => c == '\n') + 1; //The analyzer uses 1-based indexing.
                int analyzerStartColumn = analyzerStartPart.Split(Environment.NewLine).Last().Length + 1;
                int analyzerEndLine = analyzerEndPart.Count(c => c == '\n') + 1;
                int analyzerEndColumn = analyzerEndPart.Split('\n').Last().Length;
                DiagnosticResult analyzerDiagnosticResult = new DiagnosticResult(descriptor)
                    .WithLocation(analyzerStartLine, analyzerStartColumn);
                    //.WithSpan(analyzerStartLine, analyzerStartColumn, analyzerEndLine, analyzerEndColumn);
                _analyzerDiagnostics.Add(analyzerDiagnosticResult);

                //Code fix diagnostic.
                DiagnosticResult codeFixDiagnosticResult = getCodeFixDiagnostic(descriptor, AnalyzerInput).WithLocation(count);
                _codeFixDiagnostics.Add(codeFixDiagnosticResult);

                //Update the source offsets.
                analyzerInputOffset += analyzerInput.Length;
                codeFixInputOffset += codeFixInput.Length;
                codeFixExpectedOffset += codeFixExpected.Length;
                count++;

                //Truncate the source text that the regex is to work on.
                regexSource = regexSource.Substring(blockEnd);
            }
            #endregion
        }
    }
}
