using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReadmeGeneratorTask
{
    internal class PathUtils
    {
        private static readonly Regex PathSeparatorRegex = new("/+");

        public static string NormalizeAbsolute(string path) =>
            Uri.UnescapeDataString(new Uri(Path.GetFullPath(path)).AbsolutePath);

        public static string MakeRelativePath(string path, string relativeTo)
        {
            path = NormalizeAbsolute(path);
            relativeTo = NormalizeAbsolute(relativeTo);
            if (!relativeTo.EndsWith("/")) relativeTo += "/";

            if (path.StartsWith(relativeTo))
            {
                return path.Substring(relativeTo.Length);
            }

            if (Path.GetPathRoot(path) != Path.GetPathRoot(relativeTo))
            {
                throw new InvalidOperationException("Paths don't share the same root.");
            }

            var pathParts = PathSeparatorRegex.Split(path)!;
            var baseParts = PathSeparatorRegex.Split(relativeTo)!;
            var commonIndex = GetCommonBase();
            Debug.Assert(commonIndex is null or >= 0);
            if (commonIndex == null) return path;

            var remainingBaseParts = baseParts.Length - (int)commonIndex - 2;
            var relativeToBase = string.Concat(Enumerable.Repeat("../", remainingBaseParts));
            return relativeToBase + string.Join("/", pathParts.Skip((int)commonIndex + 1));

            int? GetCommonBase()
            {
                for (var i = 0; i < pathParts.Length; i++)
                {
                    if (pathParts[i] != baseParts[i])
                    {
                        return i - 1;
                    }
                }

                return null;
            }
        }
    }
}
