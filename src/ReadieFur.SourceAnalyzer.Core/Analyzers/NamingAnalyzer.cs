using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadieFur.SourceAnalyzer.Core.Config;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class NamingAnalyzer : DiagnosticAnalyzer
    {
#if NET472 && false
        //Triggers the ConfigLoader to do it's initial setup if the VSIX setup was missed (hacky workaround, shouldn't be used).
        //Volatile causes the compiler to not optimise out the variable.
        private volatile ConfigRoot _configRoot = ConfigLoader.Configuration;
#endif
        private readonly IReadOnlyDictionary<NamingConvention, DiagnosticDescriptor> _descriptors;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _descriptors.Values.ToImmutableArray();

        public NamingAnalyzer()
        {
            Dictionary<NamingConvention, DiagnosticDescriptor> descriptors = new();

            //Using reflection here is ok as it is a one-time thing, I am only using it to reduce the amount of code I have to manually write here (ideally I'd have something auto-generate static code here).
            foreach (PropertyInfo? prop in typeof(Naming).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop is null
                    || prop.PropertyType != typeof(NamingConvention)
                    //|| prop.GetCustomAttribute<AnalyzerPropertiesAttribute>() is not AnalyzerPropertiesAttribute attributeProperties
                    || !Helpers.TryGetAnalyzerID(prop.Name, out string id, out ENamingAnalyzer enumValue)
                    || prop.GetValue(ConfigLoader.Configuration.Naming) is not NamingConvention value
                    || string.IsNullOrEmpty(value.Pattern))
                    continue;

                //Make sure the regex format is valid.
                try { new Regex(value.Pattern); }
                catch { continue; }

                DiagnosticSeverity severity;
                switch (value.Severity)
                {
                    case ESeverity.None:
                        severity = DiagnosticSeverity.Hidden;
                        break;
                    case ESeverity.Info:
                        severity = DiagnosticSeverity.Info;
                        break;
                    case ESeverity.Warning:
                        severity = DiagnosticSeverity.Warning;
                        break;
                    case ESeverity.Error:
                        severity = DiagnosticSeverity.Error;
                        break;
                    default:
                        //This shouldn't be reached.
                        throw new InvalidOperationException();
                }

                descriptors.Add(value, new(
                    id: id,
                    title: $"{prop.Name} does not match the provided naming schema.",
                    messageFormat: "'{0}' does not match the regular expression '{1}'",
                    category: "Naming",
                    defaultSeverity: severity,
                    isEnabledByDefault: value.Enabled
                ));
            }

            _descriptors = descriptors;
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            /*//Don't modify auto-generated code.
            if (context.IsGeneratedCode)
                return;*/

            Analyze(context.Symbol, context.ReportDiagnostic);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not LocalDeclarationStatementSyntax node)
                return;

            foreach (VariableDeclaratorSyntax variable in node.Declaration.Variables)
            {
                if (context.SemanticModel.GetDeclaredSymbol(variable) is not ISymbol symbol
                    || symbol.Kind != SymbolKind.Local)
                    continue;

                Analyze(symbol, context.ReportDiagnostic);
            }
        }

        private void Analyze(ISymbol symbol, Action<Diagnostic> reportDiagnosticMethod)
        {
            NamingConvention? namingConvention;
            switch (symbol)
            {
                case IFieldSymbol fieldSymbol:
                    switch (fieldSymbol.DeclaredAccessibility)
                    {
                        case Accessibility.Private:
                            namingConvention = ConfigLoader.Configuration.Naming.PrivateField;
                            break;
                        case Accessibility.Internal:
                            namingConvention = ConfigLoader.Configuration.Naming.InternalField;
                            break;
                        case Accessibility.Protected:
                            namingConvention = ConfigLoader.Configuration.Naming.ProtectedField;
                            break;
                        case Accessibility.Public:
                            namingConvention = ConfigLoader.Configuration.Naming.PublicField;
                            break;
                        default:
                            //TODO: Look into the And/Or Friend values more.
                            return;
                    }
                    break;
                case IPropertySymbol:
                    namingConvention = ConfigLoader.Configuration.Naming.Property;
                    break;
                case IMethodSymbol:
                    namingConvention = ConfigLoader.Configuration.Naming.Method;
                    break;
                case INamedTypeSymbol namedTypeSymbol:
                    switch (namedTypeSymbol.TypeKind)
                    {
                        case TypeKind.Class:
                            namingConvention = ConfigLoader.Configuration.Naming.Class;
                            break;
                        case TypeKind.Interface:
                            namingConvention = ConfigLoader.Configuration.Naming.Interface;
                            break;
                        case TypeKind.Enum:
                            namingConvention = ConfigLoader.Configuration.Naming.Enum;
                            break;
                        case TypeKind.Struct:
                            namingConvention = ConfigLoader.Configuration.Naming.Struct;
                            break;
                        case TypeKind.TypeParameter:
                            namingConvention = ConfigLoader.Configuration.Naming.GenericParameter;
                            break;
                        default:
                            return;
                    }
                    break;
                case ILocalSymbol:
                    namingConvention = ConfigLoader.Configuration.Naming.LocalVariable;
                    break;
                case IParameterSymbol:
                    namingConvention = ConfigLoader.Configuration.Naming.Parameter;
                    break;
                case INamespaceSymbol:
                    namingConvention = ConfigLoader.Configuration.Naming.Namespace;
                    break;
                default:
                    return;
            }
            if (namingConvention is null
                || !namingConvention.Enabled
                || new Regex(namingConvention.Pattern).IsMatch(symbol.Name))
                return;

            reportDiagnosticMethod(Diagnostic.Create(
                _descriptors[namingConvention],
                symbol.Locations[0],
                symbol.Name,
                namingConvention.Pattern
            ));
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeSymbol,
                SymbolKind.Field, SymbolKind.Property, SymbolKind.Method, SymbolKind.NamedType, /*SymbolKind.Local,*/ SymbolKind.Parameter, SymbolKind.Namespace);
            //SymbolKind.Local is not supported by RegisterSymbolAction, it must be preprocessed through RegisterSyntaxNodeAction first.
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
        }
    }
}
