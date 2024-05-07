using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ObjectStructureAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor PropertiesAtTopDiagnosticDescriptor => new(
            id: EAnalyzerID.ObjectStructure_PropertiesAtTop.ToTag(),
            title: "Property location",
            messageFormat: "Properties should be at the top of the containing type.",
            category: "Formatting",
            defaultSeverity: Helpers.GetDiagnosticSeverity(ConfigManager.Configuration.Formatting?.ObjectStructure?.Severity),
            isEnabledByDefault: ConfigManager.Configuration.Formatting?.ObjectStructure?.PropertiesAtTop is true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(PropertiesAtTopDiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //context.RegisterSyntaxNodeAction(AnalyzePropertyPosition, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxTreeAction(AnalyzePropertyPosition);
        }

        private void AnalyzePropertyPosition(SyntaxTreeAnalysisContext context)
        {
            if (ConfigManager.Configuration.Formatting?.ObjectStructure?.PropertiesAtTop is not true)
                return;

            foreach (SyntaxNode node in context.Tree.GetRoot().ChildNodes())
            {
                if (node is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
                    foreach (SyntaxNode childNode in namespaceDeclarationSyntax.Members)
                        if (childNode is TypeDeclarationSyntax innertypeDeclarationSyntax)
                            AnalyzePropertyPositionRecursive(context, innertypeDeclarationSyntax);
                else if (node is TypeDeclarationSyntax typeDeclarationSyntax)
                    AnalyzePropertyPositionRecursive(context, typeDeclarationSyntax);
            }
        }

        private void AnalyzePropertyPositionRecursive(SyntaxTreeAnalysisContext context, TypeDeclarationSyntax typeDeclarationSyntax)
        {
            bool allPropertiesAtTop = true;
            foreach (SyntaxNode node in typeDeclarationSyntax.Members)
            {
                if (node is TypeDeclarationSyntax nestedTypeDeclarationSyntax)
                {
                    AnalyzePropertyPositionRecursive(context, nestedTypeDeclarationSyntax);
                }
                else if (node is PropertyDeclarationSyntax || node is FieldDeclarationSyntax)
                {
                    if (!allPropertiesAtTop)
                    {
                        //Get the location without the leading or trailing trivia.
                        Location location = Location.Create(context.Tree, node.Span);
                        context.ReportDiagnostic(Diagnostic.Create(PropertiesAtTopDiagnosticDescriptor, location));
                    }
                }
                else if (node is ConstructorDeclarationSyntax or DestructorDeclarationSyntax or MethodDeclarationSyntax or OperatorDeclarationSyntax)
                {
                    allPropertiesAtTop = false;
                }
            }
        }
    }
}
