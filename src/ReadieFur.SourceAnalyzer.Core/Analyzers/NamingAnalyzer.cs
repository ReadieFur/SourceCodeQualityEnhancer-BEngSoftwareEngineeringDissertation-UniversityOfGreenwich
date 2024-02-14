using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadieFur.SourceAnalyzer.Core.Config;
using System.Linq;
using static ReadieFur.SourceAnalyzer.Core.Config.ConfigMaster;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.FSharp)]
    public class NamingAnalyzer : DiagnosticAnalyzer
    {
        //https://michaelscodingspot.com/debug-3rd-party-code-dotnet/
        private readonly IReadOnlyDictionary<NamingConvention, DiagnosticDescriptor> _descriptors = Helpers.GetNamingDescriptors().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _descriptors.Values.ToImmutableArray();

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
                            namingConvention = Configuration.Naming.PrivateField;
                            break;
                        case Accessibility.Internal:
                            namingConvention = Configuration.Naming.InternalField;
                            break;
                        case Accessibility.Protected:
                            namingConvention = Configuration.Naming.ProtectedField;
                            break;
                        case Accessibility.Public:
                            namingConvention = Configuration.Naming.PublicField;
                            break;
                        default:
                            //TODO: Look into the And/Or Friend values more.
                            return;
                    }
                    break;
                case IPropertySymbol:
                    namingConvention = Configuration.Naming.Property;
                    break;
                case IMethodSymbol:
                    namingConvention = Configuration.Naming.Method;
                    break;
                case INamedTypeSymbol namedTypeSymbol:
                    switch (namedTypeSymbol.TypeKind)
                    {
                        case TypeKind.Class:
                            namingConvention = Configuration.Naming.Class;
                            break;
                        case TypeKind.Interface:
                            namingConvention = Configuration.Naming.Interface;
                            break;
                        case TypeKind.Enum:
                            namingConvention = Configuration.Naming.Enum;
                            break;
                        case TypeKind.Struct:
                            namingConvention = Configuration.Naming.Struct;
                            break;
                        case TypeKind.TypeParameter:
                            namingConvention = Configuration.Naming.GenericParameter;
                            break;
                        default:
                            return;
                    }
                    break;
                case ILocalSymbol:
                    namingConvention = Configuration.Naming.LocalVariable;
                    break;
                case IParameterSymbol:
                    namingConvention = Configuration.Naming.Parameter;
                    break;
                case INamespaceSymbol:
                    namingConvention = Configuration.Naming.Namespace;
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

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field, SymbolKind.Property, SymbolKind.Method, SymbolKind.NamedType, /*SymbolKind.Local,*/ SymbolKind.Parameter, SymbolKind.Namespace);
            //SymbolKind.Local is not supported by RegisterSymbolAction, it must be preprocessed through RegisterSyntaxNodeAction first.
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
        }
    }
}
