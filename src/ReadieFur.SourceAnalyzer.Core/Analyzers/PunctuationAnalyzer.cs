using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class PunctuationAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor SpaceDiagnosticDescriptor => new(
            id: EAnalyzerID.Punctuation_Space.ToTag(),
            title: "Operand space",
            messageFormat: "The operand '{0}' should" + (ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround?.Required is true ? "" : " not") + " have spaces either side of it.",
            category: "Formatting",
            defaultSeverity: Helpers.GetDiagnosticSeverity(ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround?.Severity),
            isEnabledByDefault: ConfigManager.Configuration.Formatting?.Punctuation?.SpaceAround is not null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SpaceDiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            //The below won't work because we need to analyze tokens and not nodes, unfortunately there is only a register node action available so we must instead analyze the entire tree instead.
            //context.RegisterSyntaxNodeAction(AnalyzeSpace, PunctuationToken.Punctuation.Values.ToArray());
            
            context.RegisterSyntaxTreeAction(Analyze);
        }

        private void Analyze(SyntaxTreeAnalysisContext context)
        {
        }
    }
}
