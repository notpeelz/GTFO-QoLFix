using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ReadmeGeneratorTask
{
    internal class MutexWrapper : IDisposable
    {
        private readonly Mutex mutex;

        private MutexWrapper(string name)
        {
            this.mutex = new Mutex(true, name, out var createdNew);
            if (!createdNew) this.mutex.WaitOne();
        }

        public void Dispose()
        {
            this.mutex.ReleaseMutex();
            this.mutex.Dispose();
        }

        public static MutexWrapper FromPath(string name, string path)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(PathUtils.NormalizeAbsolute(path)));
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
            }

            return new($"{name}_{sb}");
        }
    }
}
