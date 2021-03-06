using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace QoLFix.Generators
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HarmonyPatchAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Naming";

#pragma warning disable RS2008
        private static readonly DiagnosticDescriptor MissingPostfixError =
            new("BIE0001", "Missing postfix", "Missing postfix {0}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor MissingPrefixError =
            new("BIE0002", "Missing prefix", "Missing prefix {0}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
#pragma warning restore RS2008

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MissingPostfixError, MissingPrefixError);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeCall, SyntaxKind.InvocationExpression);
        }

        private static readonly string[] PatchMethod1Parameters = new[]
        {
            "patch",
            "methodName",
            "patchType",
            "generics",
            "prefixMethodName",
            "postfixMethodName",
        };

        private static readonly string[] PatchMethod2Parameters = new[]
        {
            "patch",
            "methodName",
            "parameters",
            "patchType",
            "generics",
            "prefixMethodName",
            "postfixMethodName",
        };

        private const byte PATCHTYPE_PREFIX = 1;
        private const byte PATCHTYPE_POSTFIX = 2;

        private static void AnalyzeCall(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is not IMethodSymbol methodSymbol) return;

            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            if (invocationExpr.Expression is not MemberAccessExpressionSyntax memberAccessExpr) return;

            var genericName = memberAccessExpr.Name as GenericNameSyntax;
            if (genericName?.Identifier.Text != "PatchMethod") return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;
            if (memberSymbol?.ContainingSymbol.ToString() != "QoLFix.BIEExtensions") return;

            // Don't bother checking explicit invocations of the
            // extension method
            if (memberSymbol.MethodKind != MethodKind.ReducedExtension) return;

            var genericArgs = genericName.TypeArgumentList.Arguments;
            if (genericArgs.Count != 1) return;
            var genericType = genericArgs[0];

            var constructedFrom = memberSymbol.GetConstructedReducedFrom();
            if (constructedFrom == null) return;
            if (!constructedFrom.IsGenericMethod || constructedFrom.TypeArguments.Length != 1) return;

            var paramNames = constructedFrom.Parameters.Select(x => x.Name).ToArray();

            if (!paramNames.SequenceEqual(PatchMethod1Parameters)
                && !paramNames.SequenceEqual(PatchMethod2Parameters)) return;

            var args = invocationExpr.ArgumentList.Arguments
                .Select(arg => new
                {
                    Arg = arg,
                    Param = context.SemanticModel.DetermineParameter(arg),
                })
                .ToDictionary(x => x.Param.Name);

            if (!GetConstantArg("methodName", out string? methodName)) return;
            if (!GetConstantArg("patchType", out byte? patchType)) return;

            var className = genericType.ToString()
                .Split('.')
                .Last()
                .Replace("`", "__");

            // Don't report warnings when we don't even have a valid method
            // to check for.
            if (string.IsNullOrEmpty(className)) return;
            if (string.IsNullOrEmpty(methodName)) return;

            if ((patchType & PATCHTYPE_PREFIX) != 0)
            {
                GetConstantArg("prefixMethodName", out string? prefixMethodName);
                CheckMethod(MissingPrefixError, prefixMethodName?.ToString() ?? $"{className}__{methodName}__Prefix");
            }

            if ((patchType & PATCHTYPE_POSTFIX) != 0)
            {
                GetConstantArg("postfixMethodName", out string? postfixMethodName);
                CheckMethod(MissingPostfixError, postfixMethodName?.ToString() ?? $"{className}__{methodName}__Postfix");
            }

            void CheckMethod(DiagnosticDescriptor descriptor, string methodName)
            {
                var methods = methodSymbol.ContainingType.GetMembers().OfType<IMethodSymbol>();

                // PatchMethod uses AccessTool.Method to find our
                // prefix/postfix, which isn't super strict about the
                // signature requirements.
                // Looking up the function by name only should be sufficient.
                var match = methods.FirstOrDefault(x => x.Name == methodName);
                if (IsMethodValid()) return;

                context.ReportDiagnostic(Diagnostic.Create(descriptor, context.Node.GetLocation(), methodName));

                bool IsMethodValid()
                {
                    // TODO: check for arguments
                    if (match == null) return false;
                    if (!match.IsStatic) return false;
                    return true;
                }
            }

            bool GetConstantArg<T>(string name, out T? value)
            {
                value = default;

                if (!args.TryGetValue(name, out var entry)) return false;

                var constant = context.SemanticModel.GetConstantValue(entry.Arg.Expression);
                if (!constant.HasValue) return false;

                if (constant.Value is not null) value = (T)constant.Value;
                return true;
            }
        }
    }
}
