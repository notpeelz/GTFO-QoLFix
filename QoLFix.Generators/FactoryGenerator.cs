using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QoLFix.Generators
{
    [Generator]
    public class FactoryGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNs))
            {
                throw new InvalidOperationException("Missing RootNamespace property");
            }

            var hintName = $"{rootNs}.GOFactory";

            var src = $@"
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Linq;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace {rootNs} {{
    [GeneratedCode(""{hintName}"", ""0.1"")]
    [CompilerGenerated]
    public static class GOFactory {{

        public static GameObject CreateObject(string name, Transform parent, params Il2CppSystem.Type[] comps)
        {{
            var go = new GameObject(name, comps);
            go.transform.SetParent(parent, false);
            return go;
        }}

        {string.Join("\n", GenerateFunctions(16))}
    }}
}}
";

            context.AddSource(hintName, SourceText.From(src, Encoding.UTF8));
        }

        private static IEnumerable<string> GenerateFunctions(int overloads)
        {
            for (var i = 0; i < overloads; i++)
            {
                var @params = Enumerable.Range(1, i + 1);
                var genericTypes = @params.Select(x => $"T{x}");
                var outParams = @params.Select(x => $"out T{x} comp{x}");
                var assignments = @params.Select(x => $"comp{x} = obj.GetComponent<T{x}>();");
                var constraints = @params.Select(x => $"where T{x} : Il2CppSystem.Object");

                yield return $@"
public static GameObject CreateObject<{Params(genericTypes)}>(string name, Transform parent, {Params(outParams)})
    {Lines(constraints)}
{{
    var obj = CreateObject(name, parent, comps: new Il2CppSystem.Type[]
    {{
        {Lines(genericTypes.Select(x => $"Il2CppType.Of<{x}>(),"), 8)}
    }});
    {Lines(assignments)}
    return obj;
}}
";

                static string Lines(IEnumerable<string> lines, int indent = 0) => string.Join("\n", lines);

                static string Params(IEnumerable<string> @params) => string.Join(", ", @params);
            }
        }
    }
}
