using Microsoft.CodeAnalysis;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;

namespace QoLFix.Generators
{
    [Generator]
    public class EmbeddedAssemblyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var file in context.AdditionalFiles)
            {
                var options = context.AnalyzerConfigOptions.GetOptions(file);

                options.TryGetValue("build_metadata.AdditionalFiles.Embed", out string shouldEmbedStr);
                if (!bool.TryParse(shouldEmbedStr, out var shouldEmbed)) continue;
                if (!shouldEmbed) continue;

                var ext = Path.GetExtension(file.Path);
                if (ext.Equals(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessAssembly(context, file.Path);
                }
            }
        }

        private static void ProcessAssembly(GeneratorExecutionContext context, string path)
        {
            // TODO: process dependencies?
            var bytes = default(byte[]);
            using (var sr = new StreamReader(path))
            using (var ms = new MemoryStream())
            {
                sr.BaseStream.CopyTo(ms);
                bytes = ms.ToArray();
            }

            var asm = Assembly.Load(bytes);
            var asmName = asm.GetName().Name;

            var fieldName = FormatFieldName(asmName);

            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNs))
            {
                throw new InvalidOperationException("Missing RootNamespace property");
            }

            var hintName = $"{rootNs}.EmbeddedAssembly_{fieldName}";

            var src = $@"
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace {rootNs}.EmbeddedResources {{
    [GeneratedCode(""{rootNs}.VersionInfo"", ""0.1"")]
    [CompilerGenerated]
    public static partial class EmbeddedAssembly {{
        [EmbeddedAssembly(""{asmName}"")]
        private static readonly byte[] {fieldName} = new byte[{bytes.Length}] {{{
            string.Join("\n", bytes.Batch(16).Select(FormatByteRow))
        }}};
    }}
}}
";

            context.AddSource(hintName, SourceText.From(src, Encoding.UTF8));
        }

        private static string FormatByteRow(IEnumerable<byte> byteRow)
        {
            return string.Join(", ", byteRow.Select(x => $"0x{x:X2}")) + ",";
        }

        private static string FormatFieldName(string fileName)
        {
            return Path.GetFileName(fileName)
                .Replace(" ", "_")
                .Replace(".", "_")
                .Replace("-", "_");
        }
    }
}
