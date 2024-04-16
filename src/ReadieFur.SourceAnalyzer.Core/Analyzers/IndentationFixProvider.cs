using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class IndentationFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => throw new NotImplementedException();

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            throw new NotImplementedException();
        }
    }
}
