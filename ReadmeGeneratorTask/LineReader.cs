using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReadmeGeneratorTask
{
    internal sealed class LineReader : TextReader, IEnumerable<string>, IDisposable
    {
        private readonly TextReader reader;

        public LineReader(TextReader reader)
        {
            this.reader = reader;
        }

        public override void Close() => this.reader.Close();

        public override int Peek() => this.reader.Peek();

        public override int Read() => this.reader.Read();

        public override int Read(char[] buffer, int index, int count) =>
            this.reader.Read(buffer, index, count);

        public override Task<int> ReadAsync(char[] buffer, int index, int count) =>
            this.reader.ReadAsync(buffer, index, count);

        public override int ReadBlock(char[] buffer, int index, int count) =>
            this.reader.ReadBlock(buffer, index, count);

        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count) =>
            this.reader.ReadBlockAsync(buffer, index, count);

        public override string ReadLine() => this.reader.ReadLine();

        public override Task<string> ReadLineAsync() => this.reader.ReadLineAsync();

        public override string ReadToEnd() => this.reader.ReadToEnd();

        public override Task<string> ReadToEndAsync() => this.reader.ReadToEndAsync();

        void IDisposable.Dispose() => this.reader.Dispose();

        public IEnumerator<string> GetEnumerator()
        {
            string line;
            while ((line = this.reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
