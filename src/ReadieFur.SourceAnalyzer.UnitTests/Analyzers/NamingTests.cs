using Microsoft.CodeAnalysis;
using ReadieFur.SourceAnalyzer.Core.Analyzers;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using System.Collections.Concurrent;
using Fixer = ReadieFur.SourceAnalyzer.UnitTests.Verifiers.CSharpCodeFixVerifier<ReadieFur.SourceAnalyzer.Core.Analyzers.NamingAnalyzer, ReadieFur.SourceAnalyzer.Core.Analyzers.NamingFixProvider>;

namespace ReadieFur.SourceAnalyzer.UnitTests.Analyzers
{
    [CTest]
    public class NamingTests : AAnalyzerTester
    {
        private ConcurrentDictionary<DiagnosticDescriptor, string> patterns = new();

        protected override FileInterpreter.AnalyzerDiagnosticCallback AnalyzerDiagnosticCallback => (diagnosticID, input) =>
        {
            IEnumerable<KeyValuePair<NamingConvention, DiagnosticDescriptor>> namingDescriptors = Core.Analyzers.Helpers.GetNamingDescriptors();
            KeyValuePair<NamingConvention, DiagnosticDescriptor>? diagnosticDescriptor = namingDescriptors.FirstOrDefault(kvp => kvp.Value.Id == diagnosticID);

            DiagnosticDescriptor descriptor = new(
                diagnosticDescriptor.Value.Value.Id,
                diagnosticDescriptor.Value.Value.Title,
                string.Format(diagnosticDescriptor.Value.Value.MessageFormat.ToString(), input, diagnosticDescriptor.Value.Key.Pattern),
                diagnosticDescriptor.Value.Value.Category,
                diagnosticDescriptor.Value.Value.DefaultSeverity,
                diagnosticDescriptor.Value.Value.IsEnabledByDefault
            );

            if (!patterns.TryAdd(descriptor, diagnosticDescriptor.Value.Key.Pattern!))
                throw new InvalidOperationException();

            return descriptor;
        };

        protected override FileInterpreter.CodeFixDiagnosticCallback CodeFixDiagnosticCallback => (diagnosticDescriptor, input) =>
        {
            if (!patterns.TryGetValue(diagnosticDescriptor, out string? pattern) || pattern is null)
                throw new NullReferenceException();

            return Fixer
                .Diagnostic(diagnosticDescriptor)
                .WithArguments(input, pattern);
        };

        [MTest] public async Task ClassNameAnalyzer() => await TestAnalyzer<NamingAnalyzer>("Naming_Class.cs");

        [MTest] public async Task ClassNameFixProvider() => await TestFixProvider<NamingAnalyzer, NamingFixProvider>("Naming_Class.cs");

        [MTest] public async Task LocalVariableFixProvider() => await TestFixProvider<NamingAnalyzer, NamingFixProvider>("Naming_Local.cs");
    }
}
