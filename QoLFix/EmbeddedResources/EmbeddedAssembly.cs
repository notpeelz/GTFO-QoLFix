using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QoLFix.EmbeddedResources
{
    public static partial class EmbeddedAssembly
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        private class EmbeddedAssemblyAttribute : Attribute
        {
            public EmbeddedAssemblyAttribute(string name)
            {
                this.Name = name;
            }

            public string Name { get; }
        }

        private static readonly Dictionary<string, (
            EmbeddedAssemblyAttribute Attribute,
            Lazy<Stream> AssemblyStream,
            Lazy<Assembly> Assembly
        )> Assemblies = new();

        static EmbeddedAssembly()
        {
            var resources = typeof(EmbeddedAssembly)
                .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .Select(x => new
                {
                    Field = x,
                    Attribute = x.GetCustomAttributes(typeof(EmbeddedAssemblyAttribute), false)
                        .Cast<EmbeddedAssemblyAttribute>()
                        .SingleOrDefault(),
                })
                .Where(x => x.Attribute != null);

            foreach (var res in resources)
            {
                Assemblies[res.Attribute.Name] = (
                    res.Attribute,
                    new Lazy<Stream>(() => GetAssemblyStream(res.Field)),
                    new Lazy<Assembly>(() => LoadAssembly(res.Field))
                );
            }
        }

        private static bool Initialized;

        public static void Initialize()
        {
            if (Initialized) return;
            Initialized = true;
            AppDomain.CurrentDomain.AssemblyResolve += AppDomain_AssemblyResolve;
        }

        private static Assembly AppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (var asmName in Assemblies.Keys)
            {
                if (args.Name == asmName || args.Name.StartsWith($"{asmName}, "))
                {
                    return GetAssembly(asmName);
                }
            }
            return null;
        }

        private static Stream GetAssemblyStream(FieldInfo fieldInfo)
        {
            var bytes = (byte[])fieldInfo.GetValue(null);
            return new MemoryStream(bytes, false);
        }

        private static Assembly LoadAssembly(FieldInfo fieldInfo)
        {
            var bytes = (byte[])fieldInfo.GetValue(null);
            var asm = Assembly.Load(bytes);
            return asm;
        }

        public static Assembly GetAssembly(string name)
        {
            if (!Assemblies.TryGetValue(name, out var entry)) return null;
            return entry.Assembly.Value;
        }
    }
}
