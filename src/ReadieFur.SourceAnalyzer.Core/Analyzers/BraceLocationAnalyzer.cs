using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Collections.Immutable;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class BraceLocationAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor DiagnosticDescriptor => new(
            id: Helpers.ANALYZER_ID_PREFIX + "0016",
            title: "Brace location",
            messageFormat:
                "Braces should be on "
                    + (ConfigManager.Configuration.Formatting.CurlyBraces.NewLine
                    ? "the line after."
                    : "the same line as")
                + " the declaring statement.",
            category: "Formatting",
            defaultSeverity: ConfigManager.Configuration.Formatting.CurlyBraces.Severity.ToDiagnosticSeverity(),
            isEnabledByDefault: ConfigManager.Configuration.Formatting.CurlyBraces.IsEnabled);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Block);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not BlockSyntax block)
                return;

            Location openBraceLocation = block.OpenBraceToken.GetLocation();
            bool isOnDeclarationLine = openBraceLocation.GetLineSpan().StartLinePosition.Line == block.Parent.GetLocation().GetLineSpan().StartLinePosition.Line;

            if ((ConfigManager.Configuration.Formatting.CurlyBraces.NewLine && isOnDeclarationLine)
                || (!ConfigManager.Configuration.Formatting.CurlyBraces.NewLine && !isOnDeclarationLine))
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, openBraceLocation));
        }
    }
}
