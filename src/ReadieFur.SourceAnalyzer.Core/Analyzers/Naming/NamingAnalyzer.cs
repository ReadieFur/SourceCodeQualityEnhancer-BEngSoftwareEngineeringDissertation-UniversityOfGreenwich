using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Reflection;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class NamingAnalyzer : DiagnosticAnalyzer
    {
        private IEnumerable<ANamingProvider> _analyzers
        {
            get
            {
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
                    if (type.GetInterface(nameof(ANamingProvider)) != null)
                        yield return (ANamingProvider)Activator.CreateInstance(type);
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _analyzers.Select(x => x.descriptor).ToImmutableArray();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //Register all analyzers.
            foreach (SymbolKind kind in Enum.GetValues(typeof(SymbolKind)))
                context.RegisterSymbolAction(AnalyzeSymbol, kind);
        }

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            foreach (ANamingProvider descriptor in _analyzers)
                descriptor.Analyze(context);
        }
    }
}
