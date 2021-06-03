using System;
using HandlebarsDotNet;

namespace ReadmeGeneratorTask
{
    internal static class HandlebarsExtensions
    {
        public static T? GetHash<T>(this Arguments args, string key)
        {
            if (!args.Hash.TryGetValue(key, out var value)) return default;
            var type = typeof(T);
            var isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            return (T)Convert.ChangeType(value, isNullable ? type.GetGenericArguments()[0] : type);
        }
    }
}
