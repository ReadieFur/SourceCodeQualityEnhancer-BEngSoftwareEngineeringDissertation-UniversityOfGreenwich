//#define THREAD_BASED

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class NamingFixProvider : CodeFixProvider
    {
        private readonly IReadOnlyDictionary<string, NamingConvention> _descriptors = NamingAnalyzer.DiagnosticDescriptors.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Key);
        public override ImmutableArray<string> FixableDiagnosticIds => _descriptors.Keys.ToImmutableArray();

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        //TODO: Check if it is ok to raise errors within the code fix provider (rather than just returning in an invalid state).
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            //Attempt to provide a code fix in this method.
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not SyntaxNode documentRoot)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                NamingConvention namingConvention = _descriptors[diagnostic.Id];

                //Check what type of node we will be working with.
                if (!Helpers.TryGetAnalyzerType(diagnostic.Id, out EAnalyzerID analyzer))
                    continue;
                Type? nodeType = analyzer switch
                {
                    //https://stackoverflow.com/questions/56676260/c-sharp-8-switch-expression-with-multiple-cases-with-same-result
                    //"or" is not to be confused with the bitwise "|" or operator which would change the result of this switch expression.
                    EAnalyzerID.Naming_PrivateField
                    or EAnalyzerID.Naming_InternalField
                    or EAnalyzerID.Naming_ProtectedField
                    or EAnalyzerID.Naming_PublicField => typeof(FieldDeclarationSyntax),
                    EAnalyzerID.Naming_Property => typeof(PropertyDeclarationSyntax),
                    EAnalyzerID.Naming_Method => typeof(MethodDeclarationSyntax),
                    EAnalyzerID.Naming_Class => typeof(ClassDeclarationSyntax),
                    EAnalyzerID.Naming_Interface => typeof(InterfaceDeclarationSyntax),
                    EAnalyzerID.Naming_Enum => typeof(EnumDeclarationSyntax),
                    EAnalyzerID.Naming_Struct => typeof(StructDeclarationSyntax),
                    EAnalyzerID.Naming_LocalVariable => typeof(VariableDeclaratorSyntax),
                    EAnalyzerID.Naming_Parameter => typeof(ParameterSyntax),
                    EAnalyzerID.Naming_Constant => typeof(FieldDeclarationSyntax),
                    EAnalyzerID.Naming_Namespace => typeof(NamespaceDeclarationSyntax),
                    EAnalyzerID.Naming_GenericParameter => typeof(TypeParameterSyntax),
                    _ => null
                };
                if (nodeType is null)
                    continue;

                //Get the declaration node that the diagnostic is associated with.
                //Reference: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix
                SyntaxNode? node = documentRoot
                    .FindToken(diagnostic.Location.SourceSpan.Start) //Get the token specified by the diagnostic location.
                    .Parent? //Get the parent of the token.
                    .AncestorsAndSelf() //This provides a "tree" of the source file tokens from the root to the location that contains the specified token.
                    .FirstOrDefault(ancestor => ancestor.GetType().Equals(nodeType)); //Filter the nodes to only include the ones that are of the type that we are interested in.
                //If the declaration is null, we can't provide a code fix (shouldn't happen).
                if (node is null)
                    continue;

                /* As a general rule, analyzers should exit as quickly as possible, doing minimal work. Visual Studio calls registered analyzers as the user edits code. Responsiveness is a key requirement.
                 * See: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix#create-tests-for-valid-declarations
                 */
                /* Due to the above rule, it will be difficult for us to provide a code fix for every diagnostic as we need to dynamically analyze if it is even possible with the provided information.
                 * It becomes difficult because we need to process the data in a way that is not immediately available and so it goes against the rule of being responsive and arguably Regex is relatively slow.
                 */
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Rename to match: {namingConvention.Pattern}",
                        createChangedSolution: ct => RenameSymbolAsync(context.Document, node, namingConvention, ct),
                        equivalenceKey: namingConvention.Pattern
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Solution> RenameSymbolAsync(Document document, SyntaxNode node, NamingConvention namingConvention, CancellationToken cancellationToken)
        {
            //TODO: Possibly move this to the above method.
            string? originalName = node switch
            {
                //TODO: Analyze each of these variables, this can occur when a variable is declared like so: public int a = 1, b = 2;
                FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Declaration.Variables.First().Identifier.Text,
                PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Identifier.Text,
                MethodDeclarationSyntax methodDeclaration => methodDeclaration.Identifier.Text,
                ClassDeclarationSyntax classDeclaration => classDeclaration.Identifier.Text,
                InterfaceDeclarationSyntax interfaceDeclaration => interfaceDeclaration.Identifier.Text,
                EnumDeclarationSyntax enumDeclaration => enumDeclaration.Identifier.Text,
                StructDeclarationSyntax structDeclaration => structDeclaration.Identifier.Text,
                VariableDeclaratorSyntax variableDeclarator => variableDeclarator.Identifier.Text,
                ParameterSyntax parameter => parameter.Identifier.Text,
                NamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Name.ToString(),
                TypeParameterSyntax typeParameter => typeParameter.Identifier.Text,
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(originalName))
                return document.Project.Solution;

            if (await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel semanticModel)
                return document.Project.Solution;

            string newName = string.Empty;

            #region Rename wrapper
#if THREAD_BASED
            Thread workerThread = new Thread(async () =>
            {
#endif
            try
            {
                //Rename the symbol.
                //TODO: Add options to the configuration.
                newName = new RegexEngine.Regex(namingConvention.Pattern!).Conform(originalName);
            }
            catch
            {
                //TODO: Impliment the various error handling.
                return document.Project.Solution;
            }
#if THREAD_BASED
            });
            workerThread.Start();
            workerThread.Join();
#else
#endif
            //Validate the new name...
            if (newName == originalName //If the name is the same, we don't need to do anything.
                || string.IsNullOrEmpty(newName) //If the name is empty, consider the result invalid.
                || !new System.Text.RegularExpressions.Regex(namingConvention.Pattern!).IsMatch(newName)) //If the name does not match the pattern (checked against a known working regex engine), consider the result invalid.
                return document.Project.Solution;
            #endregion

            /*ISymbol? symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (symbol is null)
                return document.Project.Solution;*/
            ISymbol? symbol = node switch
            {
                FieldDeclarationSyntax fieldDeclaration => semanticModel.GetDeclaredSymbol(fieldDeclaration.Declaration.Variables.First(), cancellationToken),
                PropertyDeclarationSyntax
                or MethodDeclarationSyntax
                or ClassDeclarationSyntax
                or InterfaceDeclarationSyntax
                or EnumDeclarationSyntax
                or StructDeclarationSyntax
                or VariableDeclaratorSyntax
                or ParameterSyntax
                or NamespaceDeclarationSyntax
                or TypeParameterSyntax
                _ => semanticModel.GetDeclaredSymbol(node, cancellationToken)
            };
            if (symbol is null)
                return document.Project.Solution;

            Solution newSolution;
#if VSIX
            newSolution = await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                symbol,
                default(Microsoft.CodeAnalysis.Rename.SymbolRenameOptions),
                newName,
                cancellationToken
            );
#else
            newSolution = await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                symbol,
                newName,
                document.Project.Solution.Workspace.Options,
                cancellationToken
            );
#endif
            return newSolution;
        }
    }
}
