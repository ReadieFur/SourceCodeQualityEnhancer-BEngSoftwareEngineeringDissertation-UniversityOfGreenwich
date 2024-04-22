using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class InferredFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InferredAnalyzer.AccessModifierDiagnosticDescriptor.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not SyntaxNode documentRoot)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == InferredAnalyzer.AccessModifierDiagnosticDescriptor.Id)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Infer access modifier",
                            createChangedDocument: ct => CorrectAccessModifierAsync(context.Document, /*diagnostic.AdditionalLocations.First(),*/ diagnostic.Properties, ct),
                            equivalenceKey: "Infer access modifier"
                        ),
                        diagnostic
                    );
                }
            }
        }

        private async Task<Document> CorrectAccessModifierAsync(Document document, /*Location nodeLocation,*/ ImmutableDictionary<string, string> diagnosticParms, CancellationToken cancellationToken)
        {
            //Shouldn't happen but just in case.
            if (ConfigManager.Configuration.Inferred?.AccessModifier is null
                || await document.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode documentRoot)
                return document;

            //Due to how I was detecting the locations in the analyzer for the unit tests, I will need to recalculate the location through the diagnostic properties (this is not desirable).
            if (!int.TryParse(diagnosticParms["start"], out int start) || !int.TryParse(diagnosticParms["length"], out int length))
                return document;
            SyntaxNode? node = documentRoot.FindNode(new(start, length), findInsideTrivia: true);
            //SyntaxNode? node = documentRoot.FindNode(nodeLocation.SourceSpan, findInsideTrivia: true);
            if (node is not TypeDeclarationSyntax and not MemberDeclarationSyntax)
                return document;

            SyntaxTokenList newModifiers;
            SyntaxKind[] accessModifiers = { SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword, SyntaxKind.PublicKeyword };
            if (ConfigManager.Configuration.Inferred.AccessModifier.IsInferred)
            {
                //Remove access modifiers.
                //IEnumerable<SyntaxToken> modifiers = ((SyntaxTokenList)((dynamic)node).Modifiers).Where(m => !accessModifiers.Contains(m.Kind()));
                IEnumerable<SyntaxToken> modifiers = node switch
                {
                    TypeDeclarationSyntax typeDeclarationSyntax => typeDeclarationSyntax.Modifiers.Where(m => !accessModifiers.Contains(m.Kind())),
                    FieldDeclarationSyntax fieldDeclarationSyntax => fieldDeclarationSyntax.Modifiers.Where(m => !accessModifiers.Contains(m.Kind())),
                    MethodDeclarationSyntax methodDeclarationSyntax => methodDeclarationSyntax.Modifiers.Where(m => !accessModifiers.Contains(m.Kind())),
                    _ => throw new InvalidOperationException()
                };
                newModifiers = SyntaxFactory.TokenList(modifiers);
            }
            else
            {
                //Add access modifiers.
                if (!Enum.TryParse(diagnosticParms["defaultAccessModifier"], out SyntaxKind defaultAccessModifierSyntax) || !accessModifiers.Contains(defaultAccessModifierSyntax))
                    return document;
                //newModifiers = ((SyntaxTokenList)((dynamic)node).Modifiers).Insert(0, SyntaxFactory.Token(defaultAccessModifierSyntax));
                newModifiers = node switch
                {
                    TypeDeclarationSyntax typeDeclarationSyntax => typeDeclarationSyntax.Modifiers.Insert(0, SyntaxFactory.Token(defaultAccessModifierSyntax)),
                    FieldDeclarationSyntax fieldDeclarationSyntax => fieldDeclarationSyntax.Modifiers.Insert(0, SyntaxFactory.Token(defaultAccessModifierSyntax)),
                    MethodDeclarationSyntax methodDeclarationSyntax => methodDeclarationSyntax.Modifiers.Insert(0, SyntaxFactory.Token(defaultAccessModifierSyntax)),
                    _ => throw new InvalidOperationException()
                };
            }

            //SyntaxNode newNode = (SyntaxNode)((dynamic)node).WithModifiers(newModifiers);
            SyntaxNode newNode = node switch
            {
                TypeDeclarationSyntax typeDeclarationSyntax => typeDeclarationSyntax.WithModifiers(newModifiers),
                FieldDeclarationSyntax fieldDeclarationSyntax => fieldDeclarationSyntax.WithModifiers(newModifiers),
                MethodDeclarationSyntax methodDeclarationSyntax => methodDeclarationSyntax.WithModifiers(newModifiers),
                _ => throw new InvalidOperationException()
            };
            SyntaxNode newRoot = documentRoot.ReplaceNode(node, newNode);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
