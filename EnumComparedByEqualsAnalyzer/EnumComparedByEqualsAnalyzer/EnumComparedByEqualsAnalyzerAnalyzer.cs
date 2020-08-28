using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EnumComparedByEqualsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EnumComparedByEqualsAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EnumComparedByEqualsAnalyzer";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocation = context.Node as InvocationExpressionSyntax;
            if ((invocation.Expression as MemberAccessExpressionSyntax).Name.Identifier.ValueText != "Equals" || invocation.ArgumentList.Arguments.Count != 1)
                return;

            var methodSymbol = context
                                .SemanticModel
                                .GetSymbolInfo(invocation)
                                .Symbol as IMethodSymbol;
            if (methodSymbol.ReturnType?.SpecialType != SpecialType.System_Boolean || methodSymbol.Parameters.SingleOrDefault()?.Type.SpecialType != SpecialType.System_Object)
                return;
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var invocationTarget = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
                if (invocationTarget.Type.TypeKind != TypeKind.Enum)
                    return;
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), invocation.Expression.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
