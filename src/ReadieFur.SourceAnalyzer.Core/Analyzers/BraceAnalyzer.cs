using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System.Collections.Immutable;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class BraceAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor DiagnosticDescriptor => new(
            id: EAnalyzerID.Brace_Location.ToTag(),
            title: "Brace location",
            messageFormat:
                "Braces should be on "
                    + (ConfigManager.Configuration.Formatting.CurlyBraces.NewLine
                    ? "the line after"
                    : "the same line as")
                + " the declaring statement.",
            category: "Formatting",
            defaultSeverity: ConfigManager.Configuration.Formatting.CurlyBraces.Severity.ToDiagnosticSeverity(),
            isEnabledByDefault: ConfigManager.Configuration.Formatting.CurlyBraces.IsEnabled);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);

        //Below are the syntax nodes we need to watch, the block kind is the default for {} capturing groups.
        //However some nodes such as class declarations, switch statments and namespaces need to be captured manually.
        private static readonly SyntaxKind[] SUPPORTED_SYNTAX_KINDS = [
            SyntaxKind.ClassDeclaration,
            //SyntaxKind.MethodDeclaration,
            //SyntaxKind.IfStatement,
            //SyntaxKind.ElseClause,
            //SyntaxKind.WhileStatement,
            //SyntaxKind.DoStatement,
            //SyntaxKind.LocalFunctionStatement,
            //SyntaxKind.LocalDeclarationStatement,
            SyntaxKind.SwitchStatement,
            //SyntaxKind.TryStatement,
            //SyntaxKind.CatchClause,
            //SyntaxKind.FinallyClause,
            SyntaxKind.NamespaceDeclaration,
            SyntaxKind.Block
        ];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //Unfortunatly it seems like it is not possible to register watchers for OpenBraceToken and CloseBraceToken as they dont seem to get called.
            //However I have instead found that registering for declerations and keywords that use brackets will work and I can then get the corrosponding BraceTokens from those nodes.
            context.RegisterSyntaxNodeAction(Analyze, SUPPORTED_SYNTAX_KINDS);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            //From my inspection, all of the nodes we want to look at contain the same BraceToken properties.
            //So to save typing out slightly different blocks for each type, I will cast to a dynamic type which will let me get the property without type checking.
            //The alternative is to use reflection and search for the property but this would break if I compiled to native as reflection only works within a managed runtime.
            //To add some saftey around this, I will wrap it in a try/catch block, though it should never fail unless the API changes as we should only reach this block using the supported syntax nodes defined above.
            //try { AnalyzeToken(context, (SyntaxToken)((dynamic)context.Node).OpenBraceToken); } catch { return; }

            //Additional research needs to be done to obtain the previous node as it is not yeilding desirable results.
            //try { AnalyzeToken(context, (SyntaxToken)((dynamic)context.Node).CloseBraceToken); } catch { return; }

            //Falling back to switch block as the cast to dynamic failed to compile on the VSIX build.
            //I wanted to avoid this as the types don't share a common base with the properties I want to access so it just means I have to write more to achieve the same result.
            SyntaxToken openBraceToken;
            SyntaxToken closeBraceToken;
            switch (context.Node)
            {
                case ClassDeclarationSyntax classDeclarationSyntax:
                    openBraceToken = classDeclarationSyntax.OpenBraceToken;
                    closeBraceToken = classDeclarationSyntax.CloseBraceToken;
                    break;
                case SwitchStatementSyntax switchStatementSyntax:
                    openBraceToken = switchStatementSyntax.OpenBraceToken;
                    closeBraceToken = switchStatementSyntax.CloseBraceToken;
                    break;
                case NamespaceDeclarationSyntax namespaceDeclarationSyntax:
                    openBraceToken = namespaceDeclarationSyntax.OpenBraceToken;
                    closeBraceToken = namespaceDeclarationSyntax.CloseBraceToken;
                    break;
                case BlockSyntax blockSyntax:
                    openBraceToken = blockSyntax.OpenBraceToken;
                    closeBraceToken = blockSyntax.CloseBraceToken;
                    break;
                default:
                    //Shouldn't be reached.
                    return;
            }

            AnalyzeToken(context, openBraceToken);
            AnalyzeToken(context, closeBraceToken);
        }

        private void AnalyzeToken(SyntaxNodeAnalysisContext context, SyntaxToken token)
        {
            //SyntaxToken braceTokenPreviousNode = token.GetPreviousToken();
            //SyntaxNode? braceTokenPreviousNode = token.Parent;
            
            //If the context.Node is a block, then the open brace token will always be on the same line as the declaring block as they are the same thing, in this case we need to get the parent of the block node.
            SyntaxNode? braceTokenPreviousNode = context.Node is BlockSyntax ? context.Node.Parent : token.Parent;
            
            Location braceLocation = token.GetLocation();

            bool isBraceOnParentLine = braceLocation.GetLineSpan().StartLinePosition.Line == braceTokenPreviousNode?.GetLocation().GetLineSpan().StartLinePosition.Line;
            if ((ConfigManager.Configuration.Formatting.CurlyBraces.NewLine && isBraceOnParentLine) || (!ConfigManager.Configuration.Formatting.CurlyBraces.NewLine && !isBraceOnParentLine))
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, braceLocation));
        }
    }
}
