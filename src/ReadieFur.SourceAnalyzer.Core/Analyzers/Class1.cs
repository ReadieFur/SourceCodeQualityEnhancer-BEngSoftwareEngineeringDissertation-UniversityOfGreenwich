using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class Analyzers : DiagnosticAnalyzer
    {
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "Analyzer1",
            "Type name contains lowercase letters",
            "Type name '{0}' contains lowercase letters",
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Type names should be all uppercase."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //Register all analyzers.
            //foreach (SymbolKind kind in Enum.GetValues(typeof(SymbolKind)))
            //context.RegisterSymbolAction(AnalyzeSymbol, kind);
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
                {
                    DiagnosticDescriptor rule = Rule;
                    if (namedTypeSymbol.Name.StartsWith("F"))
                    {
                        rule = new(
                            rule.Id + "F",
                            rule.Title,
                            rule.MessageFormat,
                            rule.Category,
                            DiagnosticSeverity.Error,
                            rule.IsEnabledByDefault,
                            description: "My foo property");
                    }
                    var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}