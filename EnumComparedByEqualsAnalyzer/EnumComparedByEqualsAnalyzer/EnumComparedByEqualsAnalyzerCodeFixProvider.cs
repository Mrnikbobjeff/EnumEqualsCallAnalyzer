using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumComparedByEqualsAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumComparedByEqualsAnalyzerCodeFixProvider)), Shared]
    public class EnumComparedByEqualsAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Replace with op_eq";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EnumComparedByEqualsAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach(var diagnostic  in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => ReplaceWithOpEqAsync(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
            
        }

        async Task<Solution> ReplaceWithOpEqAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var memAccess = invocation.Expression as MemberAccessExpressionSyntax;
            var eqExpression = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, memAccess.Expression, invocation.ArgumentList.Arguments.First().Expression);

            var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);

            var newRoot = syntaxRoot.ReplaceNode(invocation, eqExpression);

            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }
    }
}
