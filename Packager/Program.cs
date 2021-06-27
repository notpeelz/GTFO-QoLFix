using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using MTFO.ThunderstorePackager;

namespace Packager
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            var outputOption = new Option<DirectoryInfo>(
                "--output",
                arity: ArgumentArity.ExactlyOne,
                description: "Output folder where the packages will get saved to.").LegalFilePathsOnly();
            var readmeOption = new Option<FileInfo>(
                "--readme",
                arity: ArgumentArity.ExactlyOne,
                description: "Readme file to include in modpack package.").ExistingOnly();
            var iconOption = new Option<FileInfo>(
                "--icon",
                arity: ArgumentArity.ExactlyOne,
                description: "Icon file to include in the modpack package.").ExistingOnly();
            var nameOption = new Option<string>(
                "--name",
                arity: ArgumentArity.ExactlyOne,
                description: "Name of the modpack package.");
            var authorOption = new Option<string>(
                "--author",
                arity: ArgumentArity.ExactlyOne,
                description: "Author of the modpack package.");
            var descriptionOption = new Option<string>(
                "--description",
                arity: ArgumentArity.ZeroOrOne,
                description: "Description of the modpack package.");
            var versionOption = new Option<string>(
                "--version",
                arity: ArgumentArity.ExactlyOne,
                description: "Version of the modpack package.");
            var websiteOption = new Option<string>(
                "--website",
                arity: ArgumentArity.ZeroOrOne,
                description: "Website url of the modpack package.");
            var dependencyOption = new Option<string>(
                "--dependency",
                arity: ArgumentArity.ZeroOrMore,
                description: "Dependencies of the modpack package.");
            var rootCommand = new RootCommand
            {
                outputOption,
                readmeOption,
                iconOption,
                nameOption,
                authorOption,
                descriptionOption,
                versionOption,
                websiteOption,
                dependencyOption,
            };

            var console = new SystemConsole();
            var parser = new Parser(rootCommand);
            var result = parser.Parse(args);

            var output = result.FindResultFor(outputOption)?.GetValueOrDefault<DirectoryInfo>();
            var readme = result.FindResultFor(readmeOption)?.GetValueOrDefault<FileInfo>();
            var icon = result.FindResultFor(iconOption)?.GetValueOrDefault<FileInfo>();
            var name = result.FindResultFor(nameOption)?.GetValueOrDefault<string>();
            var author = result.FindResultFor(authorOption)?.GetValueOrDefault<string>();
            var description = result.FindResultFor(descriptionOption)?.GetValueOrDefault<string>();
            var version = result.FindResultFor(versionOption)?.GetValueOrDefault<string>();
            var website = result.FindResultFor(websiteOption)?.GetValueOrDefault<string>();
            var dependencies = result.FindResultFor(dependencyOption)?.GetValueOrDefault<string[]>();

            if (!Required(nameof(output), output)
                || !Required(nameof(readme), readme)
                || !Required(nameof(icon), icon)
                || !Required(nameof(name), name)
                || !Required(nameof(author), author)
                || !Required(nameof(version), version)) return 1;

            Directory.CreateDirectory(output.FullName);

            var pkg = new ThunderstorePackage(Path.Combine(output.FullName, "modpack.zip"), new()
            {
                Icon = new FileReference(icon.FullName),
                Readme = new FileReference(readme.FullName),
                Manifest = new()
                {
                    Name = name,
                    Namespace = author,
                    Description = description ?? "",
                    Version = version,
                    WebsiteUrl = website ?? "",
                    Dependencies = dependencies ?? Array.Empty<string>(),
                },
            });

            return 0;

            bool Required<T>(string name, [NotNull] T? value) where T : class
            {
                if (typeof(T) == typeof(string)
                    ? (string?)(object?)value != ""
                    : value != null) return true;
                console.Error.Write($"Mandatory option '--{name}' is not specified.");
                return false;
            }
        }
    }
}
