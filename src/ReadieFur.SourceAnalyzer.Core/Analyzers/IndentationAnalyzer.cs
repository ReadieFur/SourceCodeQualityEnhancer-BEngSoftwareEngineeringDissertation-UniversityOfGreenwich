using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IndentationAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor DiagnosticDescriptor => new(
            id: EAnalyzerID.Indentation.ToTag(),
            title: "Indentation",
            //messageFormat: "Indentation does not match the expected amount for the current level '{0}'.",
            messageFormat: "Indentation does not match the expected amount.",
            category: "Formatting",
            defaultSeverity: ConfigManager.Configuration.Formatting.Indentation.Severity.ToDiagnosticSeverity(),
            isEnabledByDefault: ConfigManager.Configuration.Formatting.Indentation.IsEnabled);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);
        //public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create<DiagnosticDescriptor>(); //TODO: TEMPORARY!

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxTreeAction(Analyze);
        }

        //TODO: Check indentation level for braceless statments (i.e. single operation if block bodies).
        //TODO: Update this to work on a line and not an individual token as things such as comments cause errors if not indented here and cannot be fixed by this current code.
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

                //TODO: This won't work with {} on the same line and will break the indentation level.
                bool isClosingLine = lineString[firstNonWhitespace] == '}';
                //Decrement the indentation level before checking the actual indentation IF the token is a closing token (as closing tokens should be on the previous indentation level).
                if (isClosingLine)
                    level -= lineString.Count(c => c == '}');

                //Attempt to find the node/token.
                //TODO: Clean this up (too many nested statments).
                string diagnosticType;
                Location diagnosticLocation;
                SyntaxToken? token = null;
                var a = root.DescendantTrivia(line.Span, _ => true, true);
                SyntaxNodeOrToken syntax = root.ChildThatContainsPosition(line.Start + firstNonWhitespace);
                int innerBlockCount = 0;
                if (syntax.IsNode)
                {
                    SyntaxNode node = syntax.AsNode();
                    SyntaxTrivia? trivia = null;

                    //Reduce the search area.
                    if (node.GetLocation().GetLineSpan().StartLinePosition.Line == line.LineNumber)
                    {
                        diagnosticType = nameof(SyntaxNode);
                        diagnosticLocation = node.GetLocation();
                    }
                    else
                    {
                        //TODO: Use the loop below to find the token instead of using the above search function (may be faster).
                        token = syntax.AsNode().FindToken(line.Start + firstNonWhitespace, true);

                        if (token.Value.GetLocation().GetLineSpan().StartLinePosition.Line == line.LineNumber)
                        {
                            diagnosticType = nameof(SyntaxToken);
                            diagnosticLocation = token.Value.GetLocation();
                        }
                        else
                        {
                            //Check if the text we are looking for is a trivia type instead.
                            trivia = syntax.AsNode().DescendantTrivia(_ => true, true).FirstOrDefault(t => t.GetLocation().GetLineSpan().StartLinePosition.Line == line.LineNumber && !t.IsKind(SyntaxKind.WhitespaceTrivia));

                            if (trivia != default)
                            {
                                token = null;
                                diagnosticType = nameof(SyntaxTrivia);
                                diagnosticLocation = trivia.Value.GetLocation();
                            }
                            else
                            {
                                //Something has gone wrong and we cannot work on this line (shouldn't occur).
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    token = syntax.AsToken();
                    diagnosticType = nameof(SyntaxToken);
                    diagnosticLocation = token.Value.GetLocation();
                }

                //Check if the level is correct.
                if (level * ConfigManager.Configuration.Formatting.Indentation.Size != firstNonWhitespace)
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptor,
                        diagnosticLocation,
                        new Dictionary<string, string>
                        {
                            { "level", level.ToString() },
                            { "diagnosticType", diagnosticType },
                        }.ToImmutableDictionary(),
                        level));

                if (!isClosingLine)
                    level -= lineString.Count(c => c == '}');

                level += lineString.Count(c => c == '{');
            }
        }
    }
}
