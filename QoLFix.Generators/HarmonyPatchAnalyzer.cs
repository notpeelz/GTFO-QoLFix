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
            new("QOL0001", "Missing postfix", "Missing postfix {0}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor MissingPrefixError =
            new("QOL0002", "Missing prefix", "Missing prefix {0}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
#pragma warning restore RS2008

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MissingPostfixError, MissingPrefixError);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeCall, SyntaxKind.InvocationExpression);
        }

        private static readonly string[] GenericPatchMethod1Parameters = new[]
        {
            "methodName",
            "patchType",
            "generics",
            "prefixMethodName",
            "postfixMethodName",
        };

        private static readonly string[] GenericPatchMethod2Parameters = new[]
        {
            "methodName",
            "parameters",
            "patchType",
            "generics",
            "prefixMethodName",
            "postfixMethodName",
        };

        private static readonly string[] NonGenericPatchMethod1Parameters = new[]
        {
            "classType",
            "methodName",
            "patchType",
            "generics",
            "prefixMethodName",
            "postfixMethodName",
        };

        private static readonly string[] NonGenericPatchMethod2Parameters = new[]
        {
            "classType",
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

            if (memberAccessExpr.Name.Identifier.Text != "PatchMethod") return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;
            if (memberSymbol?.ContainingSymbol.ToString() != "QoLFix.Patch") return;

            var paramNames = memberSymbol.Parameters.Select(x => x.Name).ToArray();

            var args = invocationExpr.ArgumentList.Arguments
                .Select(arg => new
                {
                    Arg = arg,
                    Param = context.SemanticModel.DetermineParameter(arg),
                })
                .ToDictionary(x => x.Param.Name);

            var isGeneric = false;
            isGeneric |= paramNames.SequenceEqual(GenericPatchMethod1Parameters);
            isGeneric |= paramNames.SequenceEqual(GenericPatchMethod2Parameters);
            if (isGeneric)
            {
                if (!memberSymbol.IsGenericMethod || memberSymbol.TypeArguments.Length != 1) return;
                if (memberAccessExpr.Name is not GenericNameSyntax genericName) return;

                var genericArgs = genericName.TypeArgumentList.Arguments;
                if (genericArgs.Count != 1) return;

                var className = genericArgs[0].ToString()
                    .Split('.')
                    .Last()
                    .Replace("`", "__");

                CheckCall(className);
                return;
            }

            var isNonGeneric = false;
            isNonGeneric |= paramNames.SequenceEqual(NonGenericPatchMethod1Parameters);
            isNonGeneric |= paramNames.SequenceEqual(NonGenericPatchMethod2Parameters);
            if (isNonGeneric)
            {
                if (!args.TryGetValue("classType", out var entry)) return;

                // Ignore anything that isn't a `typeof()` expression
                if (entry.Arg.Expression is not TypeOfExpressionSyntax typeofExpr) return;

                if (context.SemanticModel.GetSymbolInfo(typeofExpr.Type).Symbol is not ITypeSymbol typeSymbol) return;

                CheckCall(typeSymbol.Name);
                return;
            }

            void CheckCall(string className)
            {
                if (!GetConstantArg("methodName", out string? methodName)) return;
                if (!GetConstantArg("patchType", out byte? patchType)) return;

                // Don't report warnings when we don't even have a valid method
                // to check for.
                if (string.IsNullOrEmpty(className)) return;
                if (string.IsNullOrEmpty(methodName)) return;

                if ((patchType & PATCHTYPE_PREFIX) != 0)
                {
                    GetConstantArg("prefixMethodName", out string? prefixMethodName);
                    CheckMethod(MissingPrefixError, prefixMethodName ?? $"{className}__{methodName}__Prefix");
                }

                if ((patchType & PATCHTYPE_POSTFIX) != 0)
                {
                    GetConstantArg("postfixMethodName", out string? postfixMethodName);
                    CheckMethod(MissingPostfixError, postfixMethodName ?? $"{className}__{methodName}__Postfix");
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
}
