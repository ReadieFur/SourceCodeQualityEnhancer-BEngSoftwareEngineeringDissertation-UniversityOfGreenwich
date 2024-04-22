using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class InferredAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor AccessModifierDiagnosticDescriptor => new(
            id: EAnalyzerID.Inferred_AccessModifier.ToTag(),
            title: "Access modifier (inferred).",
            messageFormat: "Access modifiers should" + (ConfigManager.Configuration.Inferred?.AccessModifier?.IsInferred is true ? "" : " not") + " be inferred.",
            category: "Inferred",
            defaultSeverity: Helpers.GetDiagnosticSeverity(ConfigManager.Configuration.Inferred?.AccessModifier?.Severity),
            isEnabledByDefault: ConfigManager.Configuration.Inferred?.AccessModifier is not null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AccessModifierDiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNodeAccess, SyntaxKind.StructDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.MethodDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.DelegateDeclaration);
        }

        #region Access Modifier
        //https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/access-modifiers#class-and-struct-accessibility
        private void AnalyzeNodeAccess(SyntaxNodeAnalysisContext context)
        {
            if (ConfigManager.Configuration.Inferred?.AccessModifier is null
                || context.Node is not TypeDeclarationSyntax and not MemberDeclarationSyntax)
                return;

            SyntaxKind accessModifier;
            try
            {
                SyntaxKind[] accessModifiers = { SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword, SyntaxKind.PublicKeyword };

                //If this fails then one of the types is not a declaration statement (should not happen).
                accessModifier = ((SyntaxTokenList)((dynamic)context.Node).Modifiers).FirstOrDefault(m => accessModifiers.Contains(m.Kind())).Kind();
            }
            catch
            {
                //I've decided I don't want to catch this exception as it should not happen and if it does I want to know about it.
                throw;
                //return;
            }

            //https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/access-modifiers#class-and-struct-accessibility
            //We first need to check if our node is a child of any other declaration (besides namespaces) as this determines the default access modifier as stated by the .NET C# documentation.
            /* Default access modifiers:
             * namespace -> class: internal
             * namespace -> struct: internal
             * namespace -> delegate: internal
             * class -> class: private
             * class -> struct: private
             * struct -> struct: private
             * struct -> class: private
             * * -> field: private
             * * -> method: private
             * * -> property: private
             */
            /* Default access modifiers (simplified):
             * namespace -> *: internal
             * namespace -> * -> *: private
             */
            //We need to check if the parent is null, not just a namespace as the language syntax allows for non-block contained namespaces.
            SyntaxKind defaultAccessModifier = context.Node.Parent is NamespaceDeclarationSyntax or null ? SyntaxKind.InternalKeyword : SyntaxKind.PrivateKeyword;

            Location nodeLocation = context.Node.GetLocation();
            Location diagnosticLocation;
            switch (context.Node)
            {
                case TypeDeclarationSyntax typeDeclarationSyntax:
                    //location = typeDeclarationSyntax.Identifier.GetLocation();
                    diagnosticLocation = typeDeclarationSyntax.Keyword.GetLocation();
                    break;
                case FieldDeclarationSyntax fieldDeclarationSyntax:
                    //location = Location.Create(fieldDeclarationSyntax.SyntaxTree, fieldDeclarationSyntax.Declaration.Variables.Span);
                    diagnosticLocation = fieldDeclarationSyntax.Declaration.Type.GetLocation();
                    break;
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    diagnosticLocation = methodDeclarationSyntax.ReturnType.GetLocation();
                    break;
                default:
                    diagnosticLocation = nodeLocation;
                    break;
            }

            //If the configuration is set to require access modifiers and the node does not have one we can return a diagnostic here.
            ImmutableDictionary<string, string> props = new Dictionary<string, string>()
            {
                { "defaultAccessModifier", defaultAccessModifier.ToString() },
                { "start", nodeLocation.SourceSpan.Start.ToString() },
                { "length", nodeLocation.SourceSpan.Length.ToString() }
            }.ToImmutableDictionary();
            if (!ConfigManager.Configuration.Inferred.AccessModifier.IsInferred && accessModifier == SyntaxKind.None)
            {
                context.ReportDiagnostic(Diagnostic.Create(AccessModifierDiagnosticDescriptor, diagnosticLocation, /*additionalLocations: [nodeLocation],*/ properties: props));
            }
            //If the configuration is set to not require access modifiers we need to check if the node can be inferred with it's current access modifier.
            else if (ConfigManager.Configuration.Inferred.AccessModifier.IsInferred && accessModifier == defaultAccessModifier)
            {
                context.ReportDiagnostic(Diagnostic.Create(AccessModifierDiagnosticDescriptor, diagnosticLocation, /*additionalLocations: [nodeLocation],*/ properties: props));
            }
        }
        #endregion
    }
}
