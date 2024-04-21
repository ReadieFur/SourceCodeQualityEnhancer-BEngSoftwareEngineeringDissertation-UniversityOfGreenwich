using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamingAnalyzer : DiagnosticAnalyzer
    {
        //https://michaelscodingspot.com/debug-3rd-party-code-dotnet/
        private readonly IReadOnlyDictionary<NamingConvention, DiagnosticDescriptor> _descriptors = Helpers.GetNamingDescriptors().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _descriptors.Values.ToImmutableArray();

        public override void Initialize(AnalysisContext context)
        {
            //Don't analyze auto-generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            //Allow multiple threads to analyze the code.
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field, SymbolKind.Property, SymbolKind.Method, SymbolKind.NamedType, /*SymbolKind.Local,*/ SymbolKind.Parameter, SymbolKind.Namespace);
            //SymbolKind.Local is not supported by RegisterSymbolAction, it must be preprocessed through RegisterSyntaxNodeAction first.
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
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
                            namingConvention = ConfigManager.Configuration.Naming.PrivateField;
                            break;
                        case Accessibility.Internal:
                            namingConvention = ConfigManager.Configuration.Naming.InternalField;
                            break;
                        case Accessibility.Protected:
                            namingConvention = ConfigManager.Configuration.Naming.ProtectedField;
                            break;
                        case Accessibility.Public:
                            namingConvention = ConfigManager.Configuration.Naming.PublicField;
                            break;
                        default:
                            //TODO: Look into the And/Or Friend values more.
                            return;
                    }
                    break;
                case IPropertySymbol:
                    namingConvention = ConfigManager.Configuration.Naming.Property;
                    break;
                case IMethodSymbol:
                    namingConvention = ConfigManager.Configuration.Naming.Method;
                    break;
                case INamedTypeSymbol namedTypeSymbol:
                    switch (namedTypeSymbol.TypeKind)
                    {
                        case TypeKind.Class:
                            namingConvention = ConfigManager.Configuration.Naming.Class;
                            break;
                        case TypeKind.Interface:
                            namingConvention = ConfigManager.Configuration.Naming.Interface;
                            break;
                        case TypeKind.Enum:
                            namingConvention = ConfigManager.Configuration.Naming.Enum;
                            break;
                        case TypeKind.Struct:
                            namingConvention = ConfigManager.Configuration.Naming.Struct;
                            break;
                        case TypeKind.TypeParameter:
                            namingConvention = ConfigManager.Configuration.Naming.GenericParameter;
                            break;
                        default:
                            return;
                    }
                    break;
                case ILocalSymbol:
                    namingConvention = ConfigManager.Configuration.Naming.LocalVariable;
                    break;
                case IParameterSymbol:
                    namingConvention = ConfigManager.Configuration.Naming.Parameter;
                    break;
                case INamespaceSymbol:
                    namingConvention = ConfigManager.Configuration.Naming.Namespace;
                    break;
                default:
                    return;
            }

            if (namingConvention is null || !namingConvention.IsEnabled)
                return;

            Match match = Regex.Match(symbol.Name, namingConvention.Pattern);
            if (match.Success && match.Value.Length == symbol.Name.Length)
                return;

            Diagnostic diagnostic = Diagnostic.Create(
                _descriptors[namingConvention],
                symbol.Locations[0],
                symbol.Name,
                namingConvention.Pattern
            );

            reportDiagnosticMethod(diagnostic);
        }
    }
}
