using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HandlebarsDotNet;
using HandlebarsDotNet.IO;
using Microsoft.Build.Framework;

namespace ReadmeGeneratorTask
{
    [LoadInSeparateAppDomain]
    public class CreateReadme : MarshalByRefObject, ITask
    {
        private const string EVENT_CODE_MSBUILDOUTPUTPATH_NOT_FOUND = "QOLRG0001";
        private const string EVENT_CODE_TPL_PATH_NOT_FOUND = "QOLRG0002";
        private const string EVENT_CODE_INVALID_HEADER_LEVEL = "QOLRG0003";
        private const string EVENT_CODE_TPL_RENDER_ERROR = "QOLRG0004";
        private const string EVENT_CODE_NO_OUTPUTPATH_OVERRIDE = "QOLRG0005";

        private readonly IHandlebars hbs;
        private readonly Dictionary<string, HandlebarsTemplate<TextWriter, object, object>> partials = new();

        public CreateReadme()
        {
            this.hbs = Handlebars.Create(new() { ThrowOnUnresolvedBindingExpression = true });

            this.hbs.RegisterHelper("equals", (output, options, ctx, args) =>
            {
                var left = args.At<string>(0);
                var right = args[1] as string ?? "";
                if (left == right) options.Template(output, ctx);
                else options.Inverse(output, ctx);
            });

            this.hbs.RegisterHelper("notEquals", (output, options, ctx, args) =>
            {
                var left = args.At<string>(0);
                var right = args[1] as string ?? "";
                if (left != right) options.Template(output, ctx);
                else options.Inverse(output, ctx);
            });

            for (var i = 0; i < 6; i++)
            {
                var level = i + 1;
                void HeaderHelper(in EncodedTextWriter output, in HelperOptions options, in Context ctx, in Arguments args)
                {
                    var text = args.At<string>(0);
                    var headerLevel = options.Data["__headerLevel"] as int?;
                    headerLevel = level + (headerLevel ?? 1) - 1;
                    if (headerLevel > 6)
                    {
                        this.BuildEngine.LogWarningEvent(new(
                            code: EVENT_CODE_INVALID_HEADER_LEVEL,
                            message: "Header level exceeds the maximum level (h6).",
                            subcategory: null,
                            file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                            helpKeyword: null,
                            senderName: nameof(CreateReadme)));
                        headerLevel = 6;
                    }

                    output.Write(new string('#', (int)headerLevel));
                    output.Write(' ');
                    output.Write(text);
                };
                this.hbs.RegisterHelper($"h{level}", HeaderHelper);
            }

            this.hbs.RegisterHelper("concat", (output, ctx, args) => output.Write(string.Concat(args)));

            void PartialHelper(in EncodedTextWriter output, in HelperOptions options, in Context ctx, in Arguments args)
            {
                var partialCtx = new Dictionary<string, object>((Dictionary<string, object>)args[1]);

                var path = args.At<string>(0);
                if (!Path.IsPathRooted(path))
                {
                    var tplDir = (string)partialCtx["__TEMPLATE__"];
                    // If we're rendering a partial, make the path relative to
                    // the partial's location.
                    if (partialCtx.TryGetValue("__PARTIAL__", out var partialPath))
                    {
                        tplDir = (string)partialPath;
                    }
                    tplDir = Path.GetDirectoryName(tplDir);
                    path = Path.Combine(tplDir, path);
                }

                // Normalize for the dictionary key
                path = PathUtils.NormalizeAbsolute(path);
                partialCtx["__PARTIAL__"] = path;

                if (!this.partials.TryGetValue(path, out var partial))
                {
                    if (!this.TryReadTemplate(path, out var file)) return;
                    using var sr = new NoEolStreamReader(file!);
                    var tpl = this.hbs.Compile(sr);
                    partial = this.partials[path] = tpl;
                }

                // FIXME: ideally we'd clone the DataValues dictionary, but
                // HandleBarsDotNet doesn't have anything for that :(
                var data = new Dictionary<string, object>
                {
                    ["__headerLevel"] = options.Data["__headerLevel"]
                };

                partial(output.CreateWrapper(), partialCtx, data);
            }
            this.hbs.RegisterHelper("embedPartial", PartialHelper);

            this.hbs.RegisterHelper("headerLevel", (output, options, ctx, args) =>
            {
                var headerLevel = args.At<int>(0);
                var frame = options.CreateFrame(ctx);
                var data = new HandlebarsDotNet.ValueProviders.DataValues(frame);
                data["__headerLevel"] = headerLevel;
                options.Template(output, frame);
            });

            this.hbs.RegisterHelper("embedVideo", (output, ctx, args) =>
            {
                var outputPath = ctx.GetValue<string>("__OUTPUT__");
                var isThunderstore = ctx.GetValue<string>("release") == "thunderstore";
                var name = args.GetHash<string>("name");
                var ext = args.GetHash<string>("ext") ?? "jpg";
                var height = args.GetHash<int?>("height");
                var url = args.GetHash<string>("url");

                var imgPath = $"img/{name}_thumbnail.{ext}";
                if (isThunderstore)
                {
                    imgPath = $"{ctx["REPO_URL"]}/raw/master/{imgPath}";
                }
                else
                {
                    imgPath = Path.Combine(this.RepositoryRootPath, imgPath);
                    imgPath = PathUtils.MakeRelativePath(imgPath, Path.GetDirectoryName(outputPath));
                }

                if (height != null && !isThunderstore)
                {
                    output.WriteSafeString("<a href=\"");
                    output.Write(url);
                    output.WriteSafeString("\"><img height=\"");
                    output.Write(height);
                    output.WriteSafeString("\" src=\"");
                    output.Write(imgPath);
                    output.WriteSafeString("\"></a>");
                    return;
                }

                output.Write($"[![{name}]({imgPath})]({url})");
            });

            this.hbs.RegisterHelper("embedImage", (output, ctx, args) =>
            {
                var outputPath = ctx.GetValue<string>("__OUTPUT__");
                var isThunderstore = ctx.GetValue<string>("release") == "thunderstore";
                var name = args.GetHash<string>("name");
                var ext = args.GetHash<string>("ext") ?? "jpg";
                var height = args.GetHash<int?>("height");

                var imgPath = $"img/{name}.{ext}";
                if (isThunderstore)
                {
                    imgPath = $"{ctx["REPO_URL"]}/raw/master/{imgPath}";
                }
                else
                {
                    imgPath = Path.Combine(this.RepositoryRootPath, imgPath);
                    imgPath = PathUtils.MakeRelativePath(imgPath, Path.GetDirectoryName(outputPath));
                }

                if (height != null && !isThunderstore)
                {
                    output.WriteSafeString("<img height=\"");
                    output.Write(height);
                    output.WriteSafeString("\" src=\"");
                    output.Write(imgPath);
                    output.WriteSafeString("\">");
                    return;
                }

                output.Write($"![{name}]({imgPath})");
            });

            this.hbs.RegisterHelper("indent", (output, options, ctx, args) =>
            {
                var indent = Math.Max(args.At<int>(0), 0);

                using var textWriter = ReusableStringWriter.Get();
                var encodedTextWriter = new EncodedTextWriter(textWriter, null, FormatterProvider.Current, true);
                options.Template(encodedTextWriter, ctx);

                using var lr = new LineReader(new StringReader(textWriter.ToString()));
                foreach (var line in lr)
                {
                    output.Write(new string(' ', indent));
                    output.Write(line);
                    output.Write("\n");
                }
            });

            this.hbs.RegisterHelper("error", (output, ctx, args) =>
            {
                var msg = args.At<string>(0);
                this.BuildEngine.LogErrorEvent(new(
                    code: EVENT_CODE_TPL_RENDER_ERROR,
                    message: $"An error occurred while rendering the template: {msg}",
                    subcategory: null,
                    file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                    helpKeyword: null,
                    senderName: nameof(CreateReadme)));
            });
        }

        public IBuildEngine BuildEngine { get; set; } = default!;

        public ITaskHost? HostObject { get; set; }

        [Required]
        public string RepositoryRootPath { get; set; } = default!;

        [Required]
        public string MSBuildOutputPath { get; set; } = default!;

        [Required]
        public string MSBuildProjectDirectory { get; set; } = default!;

        public ITaskItem[]? Context { get; set; }

        public ITaskItem[]? Partials { get; set; }

        public string[]? MetadataVars { get; set; }

        [Required]
        public ITaskItem[] Templates { get; set; } = default!;

        [Output]
        public string[]? GeneratedFiles { get; set; }

        public bool OverrideOutputPath { get; set; }

        public string? OutputPath { get; set; }

        public bool Execute()
        {
            // Make sure that we're not accidentally creating the build dir
            // at the wrong step in the MSBuild pipeline.
            if (!Directory.Exists(this.MSBuildOutputPath))
            {
                this.BuildEngine.LogErrorEvent(new(
                    code: EVENT_CODE_MSBUILDOUTPUTPATH_NOT_FOUND,
                    message: $"MSBuildOutputPath doesn't exist: {this.MSBuildOutputPath}",
                    subcategory: null,
                    file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                    helpKeyword: null,
                    senderName: nameof(CreateReadme)));
                return false;
            }

            static string AppendPathSeparator(string s) =>
                !s.EndsWith("/") && !s.EndsWith("\\") ? s + "/" : s;

            this.MSBuildProjectDirectory = AppendPathSeparator(this.MSBuildProjectDirectory);
            this.MSBuildOutputPath = AppendPathSeparator(this.MSBuildOutputPath);

            var taskOutputPath = this.MSBuildProjectDirectory;
            if (!this.OverrideOutputPath)
            {
                taskOutputPath = Path.Combine(this.MSBuildOutputPath, this.OutputPath?.TrimStart('/') ?? "");
            }
            else if (string.IsNullOrEmpty(this.OutputPath))
            {
                this.BuildEngine.LogErrorEvent(new(
                    code: EVENT_CODE_NO_OUTPUTPATH_OVERRIDE,
                    message: "OverrideOutputPath requires OutputPath to be set.",
                    subcategory: null,
                    file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                    helpKeyword: null,
                    senderName: nameof(CreateReadme)));
                return false;
            }
            else
            {
                taskOutputPath = Path.Combine(taskOutputPath, this.OutputPath ?? "");
            }

            taskOutputPath = PathUtils.NormalizeAbsolute(taskOutputPath);

            var files = new List<string>();

            if (this.Partials != null)
            {
                foreach (var partial in this.Partials)
                {
                    var path = partial.GetMetadata("Path");
                    if (string.IsNullOrEmpty(path)) continue;
                    if (!this.AddPartial(partial.ItemSpec, path)) return false;
                }
            }

            foreach (var template in this.Templates)
            {
                var tplInPath = template.ItemSpec;
                var tplFileName = Path.GetFileName(tplInPath);

                var tplOutPaths = template.GetMetadata("OutputPath").Split(';');

                if (string.IsNullOrEmpty(tplOutPaths[0]))
                {
                    tplOutPaths[0] = taskOutputPath;
                }

                for (var i = 0; i < tplOutPaths.Length; i++)
                {
                    if (tplOutPaths[i].EndsWith("/") || tplOutPaths[i].EndsWith("\\"))
                    {
                        tplOutPaths[i] = Path.Combine(tplOutPaths[i], tplFileName);
                    }
                }

                foreach (var path in tplOutPaths)
                {
                    using var mutex = MutexWrapper.FromPath("QOLRG", path);
                    if (!this.BuildTemplate(tplInPath, path)) return false;
                    files.Add(path);
                }
            }

            this.GeneratedFiles = files.ToArray();

            return true;
        }

        private bool AddPartial(string name, string templatePath)
        {
            if (!this.TryReadTemplate(templatePath, out var file)) return false;
            using var sr = new NoEolStreamReader(file!);
            var tpl = this.hbs.Compile(sr);
            this.hbs.RegisterTemplate(name, tpl);
            return true;
        }

        // XXX: netstandard2.0 doesn't have
        // System.Diagnostics.CodeAnalysis.NotNullWhenAttribute :(
        private bool TryReadTemplate(string path, out FileStream? file)
        {
            file = null;
            try
            {
                file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (DirectoryNotFoundException) { NotFound(); return false; }
            catch (FileNotFoundException) { NotFound(); return false; }

            return true;

            void NotFound() =>
                this.BuildEngine.LogErrorEvent(new(
                    code: EVENT_CODE_TPL_PATH_NOT_FOUND,
                    message: $"Template file not found: {path}",
                    subcategory: null,
                    file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0,
                    helpKeyword: null,
                    senderName: nameof(CreateReadme)));
        }

        private readonly Dictionary<string, HandlebarsTemplate<TextWriter, object?, object>> templates = new();

        private bool BuildTemplate(string templatePath, string outPath)
        {
            if (!this.templates.TryGetValue(templatePath, out var tpl))
            {
                if (!this.TryReadTemplate(templatePath, out var inFile)) return false;

                using var sr = new NoEolStreamReader(inFile!);
                tpl = this.hbs.Compile(sr);
                this.templates[templatePath] = tpl;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            using var sw = new StreamWriter(File.Open(outPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                NewLine = "\n",
            };

            var ctx = CreateContext();

            var relPath = PathUtils.MakeRelativePath(templatePath, this.RepositoryRootPath);
            sw.Write($"[//]: # (THIS FILE WAS GENERATED FROM {relPath})\n");
            if (this.MetadataVars != null)
            {
                foreach (var v in this.MetadataVars)
                {
                    if (!ctx.TryGetValue(v, out var value)) continue;
                    sw.Write($"[//]: # ({v}: {value})\n");
                }
            }
            sw.Write("\n");

            tpl(sw, ctx);

            return true;

            Dictionary<string, object?> CreateContext()
            {
                var ctx = new Dictionary<string, object?>
                {
                    { "__TEMPLATE__", templatePath },
                    { "__OUTPUT__", outPath },
                };

                if (this.Context == null) return ctx;
                foreach (var entry in this.Context)
                {
                    var key = entry.ItemSpec;
                    var rawValue = entry.GetMetadata("Value");
                    var type = entry.GetMetadata("Type");

                    ctx[key] = type switch
                    {
                        "json" => DeserializeJson(rawValue),
                        _ => rawValue,
                    };
                }

                return ctx;
            }
        }

        private static object? DeserializeJson(string json)
        {
            var document = JsonDocument.Parse(json, new()
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            return JsonElement(document.RootElement);

            static object JsonObject(JsonElement element)
            {
                var obj = new Dictionary<string, object?>();
                foreach (var e in element.EnumerateObject())
                {
                    obj.Add(e.Name, JsonElement(e.Value));
                }
                return obj;
            }

            static List<object?> JsonArray(JsonElement element)
            {
                var arr = new List<object?>();
                foreach (var e in element.EnumerateArray())
                {
                    arr.Add(JsonElement(e));
                }
                return arr.ToList();
            }

            static object? JsonElement(JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Object:
                        return JsonObject(element);
                    case JsonValueKind.Array:
                        return JsonArray(element);
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.Number:
                        if (element.TryGetInt64(out var value)) return value;
                        return element.GetDouble();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    default:
                        throw new InvalidOperationException($"Invalid JsonValueKind: {element.ValueKind}");
                }
            }
        }
    }
}
