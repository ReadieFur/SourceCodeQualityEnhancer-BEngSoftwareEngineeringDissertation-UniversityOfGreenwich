using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    internal class ObjectStructureFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ObjectStructureAnalyzer.PropertiesAtTopDiagnosticDescriptor.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (ConfigManager.Configuration.Formatting?.ObjectStructure?.PropertiesAtTop is true)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                        title: "Move property to the top of the containing type.",
                        //Use createChangedDocument as we only want to update the document in this case instead of providing a solution fix.
                        createChangedDocument: cancellationToken => MovePropertyToTopAsync(context.Document, diagnostic.Location, cancellationToken),
                        equivalenceKey: "Object structure"),
                    diagnostic);
                }
                
            }

            await Task.CompletedTask;
        }

        private async Task<Document> MovePropertyToTopAsync(Document document, Location location, CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode node = root.FindNode(location.SourceSpan);
            if (node.Parent is not TypeDeclarationSyntax containingType || node is not MemberDeclarationSyntax member)
                return document;

            SyntaxNode? firstMember = containingType.Members.FirstOrDefault();

            SyntaxNode newRoot = root.TrackNodes(member, containingType, firstMember);
            member = newRoot.GetCurrentNode(member);
            newRoot = newRoot.RemoveNode(member, SyntaxRemoveOptions.KeepNoTrivia);
            
            firstMember = newRoot.GetCurrentNode(firstMember);
            containingType = newRoot.GetCurrentNode(containingType);
            if (firstMember is null)
            {
                newRoot = newRoot.ReplaceNode(containingType, containingType.AddMembers(member));
            }
            else
            {
                newRoot = newRoot.InsertNodesBefore(firstMember, [member]);
            }

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
