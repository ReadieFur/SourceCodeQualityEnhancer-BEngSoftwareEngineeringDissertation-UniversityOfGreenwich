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

        public static DiagnosticDescriptor NewKeywordDiagnosticDescriptor => new(
            id: EAnalyzerID.Inferred_NewKeyword.ToTag(),
            title: "New keyword (inferred).",
            messageFormat: "The new keyword for constructors should" + (ConfigManager.Configuration.Inferred?.Constructor?.IsInferred is true ? "" : " not") + " be inferred.",
            category: "Inferred",
            defaultSeverity: Helpers.GetDiagnosticSeverity(ConfigManager.Configuration.Inferred?.AccessModifier?.Severity),
            isEnabledByDefault: ConfigManager.Configuration.Inferred?.AccessModifier is not null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AccessModifierDiagnosticDescriptor, NewKeywordDiagnosticDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNodeAccess, SyntaxKind.StructDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.MethodDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.DelegateDeclaration);
            //context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            //ArgumentSyntax needs to be checked via the semantic model because we need access to the underlying method.
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            foreach (SyntaxNode node in context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes(_ => true))
            {
                AnalyzeObjectCreation(context, node);
            }
        }

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
                //accessModifier = ((SyntaxTokenList)((dynamic)context.Node).Modifiers).FirstOrDefault(m => accessModifiers.Contains(m.Kind())).Kind();
                accessModifier = context.Node switch
                {
                    TypeDeclarationSyntax typeDeclarationSyntax => typeDeclarationSyntax.Modifiers.FirstOrDefault(m => accessModifiers.Contains(m.Kind())).Kind(),
                    FieldDeclarationSyntax fieldDeclarationSyntax => fieldDeclarationSyntax.Modifiers.FirstOrDefault(m => accessModifiers.Contains(m.Kind())).Kind(),
                    MethodDeclarationSyntax methodDeclarationSyntax => methodDeclarationSyntax.Modifiers.FirstOrDefault(m => accessModifiers.Contains(m.Kind())).Kind(),
                    _ => throw new InvalidOperationException()
                };
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

        private void AnalyzeObjectCreation(SemanticModelAnalysisContext context, SyntaxNode node)
        {
            /* Prerequisites:
             * Config is defined.
             * Node if of type ObjectCreationExpressionSyntax.
             * Languange version is C# 9 or higher (required for implicit object creation) --Checked later for implicit to explicit conversion-- (can't work as it won't compile so this diagnostic wont be reached).
             */
            if (ConfigManager.Configuration.Inferred?.Constructor is null
                || node is not ObjectCreationExpressionSyntax
                || context.SemanticModel.SyntaxTree.Options is not CSharpParseOptions options
                || options.LanguageVersion < LanguageVersion.CSharp9)
                return;

            //Attempt to locate the variable that this object is being assigned to IF it is being assigned to a variable.
            /* Examples:
             * var a = new A(); //Check - EqualsValueClauseSyntax.
             * new A(); //Ignore - ExpressionStatementSyntax.
             * Foo(new A()); //Check (if no overloads) - ArgumentSyntax.
             */
            Location diagnosticLocation = node.GetLocation();
            ITypeSymbol underlyingType;
            Location underlyingTypeLocation;
            ITypeSymbol declaredType;
            ILocalSymbol? localSymbol = null;
            switch (node.Parent)
            {
                case EqualsValueClauseSyntax equalsValueClauseSyntax:
                    {
                        switch (equalsValueClauseSyntax.Parent)
                        {
                            case VariableDeclaratorSyntax variableDeclaratorSyntax:
                                {
                                    if (context.SemanticModel.GetDeclaredSymbol(variableDeclaratorSyntax) is not ILocalSymbol _localSymbol)
                                        return;

                                    localSymbol = _localSymbol;
                                    //diagnosticLocation = variableDeclaratorSyntax.GetLocation();
                                    underlyingType = localSymbol.Type;
                                    //The previous token of the identifier is the type (or var).
                                    underlyingTypeLocation = variableDeclaratorSyntax.Identifier.GetPreviousToken().GetLocation();
                                    var a = context.SemanticModel.SyntaxTree.GetRoot().FindToken(underlyingTypeLocation.SourceSpan.Start);
                                    declaredType = context.SemanticModel.GetTypeInfo(equalsValueClauseSyntax.Value, context.CancellationToken).Type;
                                    break;
                                }
                            default:
                                return;
                        }
                        break;
                    }
                /*case ExpressionStatementSyntax expressionStatementSyntax:
                    break;*/
                case ArgumentSyntax argumentSyntax:
                    {
                        if (argumentSyntax.Parent is not ArgumentListSyntax argumentListSyntax
                            || argumentListSyntax.Parent is not InvocationExpressionSyntax invocationExpressionSyntax)
                            return;

                        //We need to check if the method has overloads and if this is the only overload.
                        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax, context.CancellationToken);
                        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                            return;

                        /*//If the method has overloads we need to check if this is the only overload.
                        if (methodSymbol.ContainingType.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>().Count() > 1)
                            return;*/

                        //Check that the base parameter matches the declared type.
                        int parameterIndex = Math.Min(argumentListSyntax.Arguments.IndexOf(argumentSyntax), methodSymbol.Parameters.Length - 1);
                        if (parameterIndex < 0)
                            return;

                        //diagnosticLocation = argumentSyntax.GetLocation();
                        underlyingType = methodSymbol.Parameters[parameterIndex].Type;
                        underlyingTypeLocation = methodSymbol.Parameters[parameterIndex].Locations.FirstOrDefault();
                        declaredType = context.SemanticModel.GetTypeInfo(argumentSyntax.Expression, context.CancellationToken).Type;
                        break;
                    }
                default:
                    return;
            }

            //If the underlying type matches the object creation type then we can return a diagnostic here.
            //Additionally if the underlying type is a var keyword then we can also return a diagnostic here with an additional location to change the var keyword to be explicit.
            //Using the .Equals or == operator does not work for ITypeSymbol so we need to use the SymbolEqualityComparer.
            //https://github.com/dotnet/roslyn-analyzers/issues/3427
            bool typesMatch = SymbolEqualityComparer.Default.Equals(underlyingType, declaredType);

            //The localSymbol is of a private sealed type so we can't check for this type staticly, so instead I will use reflection to check for the value (as access modifiers in the VM are "just a suggestion").
            //Microsoft.CodeAnalysis.CSharp.Symbols.SourceLocalSymbol.LocalWithInitializer //Line 309 -> public bool IsVar { get; }
            bool? isVar = localSymbol?.GetType().GetProperty("IsVar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.GetValue(localSymbol) as bool?;

            //TODO: Temporary workaround for diagnostic analyzer with additional locations.
            ImmutableDictionary<string, string> parameters = new Dictionary<string, string>()
            {
                { "underlyingStart", underlyingTypeLocation.SourceSpan.Start.ToString() },
                { "underlyingLength", underlyingTypeLocation.SourceSpan.Length.ToString() }
            }.ToImmutableDictionary();

            //I think this is correct?
            if (ConfigManager.Configuration.Inferred?.Constructor?.IsInferred is true)
            {
                if (typesMatch && isVar is true)
                {
                    //Remove the var keyword and replace it with the underlying type.
                    //Remove the explicit object creation type and replace it with an implicit object creation type.
                    context.ReportDiagnostic(Diagnostic.Create(NewKeywordDiagnosticDescriptor, diagnosticLocation, properties: parameters));
                }
                else if (typesMatch)
                {
                    //Remove the explicit object creation type and replace it with an implicit object creation type.
                    context.ReportDiagnostic(Diagnostic.Create(NewKeywordDiagnosticDescriptor, diagnosticLocation));
                }
            }
            else
            {
                if (isVar is false)
                {
                    //Make the object definition a var.
                    //Make the object creation type explicit.
                    context.ReportDiagnostic(Diagnostic.Create(NewKeywordDiagnosticDescriptor, diagnosticLocation, properties: parameters));
                }
            }
        }
    }
}
