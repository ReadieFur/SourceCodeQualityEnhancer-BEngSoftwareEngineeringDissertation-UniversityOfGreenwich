using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IndentationAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor DiagnosticDescriptor => new(
            id: Helpers.ANALYZER_ID_PREFIX + "0017",
            title: "Indentation",
            //messageFormat: "Indentation does not match the expected amount for the current level '{0}'.",
            messageFormat: "Indentation does not match the expected amount.",
            category: "Formatting",
            defaultSeverity: ConfigManager.Configuration.Formatting.Indentation.Severity.ToDiagnosticSeverity(),
            isEnabledByDefault: ConfigManager.Configuration.Formatting.Indentation.IsEnabled);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.LineDirectiveTrivia, SyntaxKind.EndOfLineTrivia, SyntaxKind.WhitespaceTrivia);
            context.RegisterSyntaxTreeAction(Analyze);
        }

        //private void Analyze(SyntaxNodeAnalysisContext context)
        private void Analyze(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetRoot();
            TextLineCollection lines = context.Tree.GetText().Lines;

            int level = 0;
            foreach (TextLine line in lines)
            {
                string lineString = line.ToString();

                //Get the staring position of the first non-whitespace token.
                int firstNonWhitespace = -1;
                for (int i = 0; i < lineString.Length; i++)
                {
                    if (char.IsWhiteSpace(lineString[i]))
                        continue;
                    
                    firstNonWhitespace = i;
                    break;
                }

                //Can occur when the line is empty (e.g. EOF).
                if (firstNonWhitespace == -1)
                    continue;

                //Attempt to find the node/token.
                SyntaxToken token;
                SyntaxNodeOrToken syntax = root.ChildThatContainsPosition(line.Start + firstNonWhitespace);
                int innerBlockCount = 0;
                if (syntax.IsNode)
                {
                    //TODO: Use the loop below to find the token instead of using the above search function (may be faster).
                    //token = root.FindToken(line.Start + firstNonWhitespace, true);
                    //Reduce the search area by using the syntax node as the search domain.
                    token = syntax.AsNode().FindToken(line.Start + firstNonWhitespace, true);

                    //Nodes contain tokens so we need to check if any of the nodes direct decendants have any open braces that we need to track.
                    //This is a required function as the source code could have a brace on the same line as it's declaring node.
                    foreach (SyntaxToken nodeToken in syntax.AsNode().ChildTokens())
                    {
                        //Limit search to nodes on the same line as the current text line (as we don't want duplicate entries from the line-by-line check.
                        if (nodeToken.GetLocation().GetLineSpan().StartLinePosition.Line != line.LineNumber)
                            continue;

                        //If the child token happens to match the current token then skip it as we work on this token in the next part (avoid duplicates).
                        if (nodeToken.Equals(token))
                            continue;

                        if (nodeToken.IsKind(SyntaxKind.OpenBraceToken))
                            innerBlockCount++;
                        else if (nodeToken.IsKind(SyntaxKind.CloseBraceToken))
                            innerBlockCount--;
                    }
                }
                else
                {
                    token = syntax.AsToken();
                }

                //Decrement the indentation level before checking the actual indentation IF the token is a closing token (as closing tokens should be on the previous indentation level).
                //TODO: Check indentation level for braceless statments (i.e. single operation if block bodies).
                //TODO: Update this to work on a line and not an individual token as things such as comments cause errors if not indented here and cannot be fixed by this current code.
                if (token.IsKind(SyntaxKind.CloseBraceToken))
                    level--;
                if (innerBlockCount < 0)
                    level += innerBlockCount;

                //Check if the level is correct.
                if (level * ConfigManager.Configuration.Formatting.Indentation.Size != firstNonWhitespace)
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptor,
                        token.GetLocation(),
                        new Dictionary<string, string> { { "level", level.ToString() } }.ToImmutableDictionary(),
                        level));

                //Increment the indentation level after checking the actual indentation IF the token is an opening token.
                //TODO: Possibly update indentation for curly and square brackets.
                if (token.IsKind(SyntaxKind.OpenBraceToken))
                    level++;
                if (innerBlockCount > 0)
                    level += innerBlockCount;
            }
        }
    }
}
