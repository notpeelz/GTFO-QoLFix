using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ReadmeGeneratorTask
{
    internal class NoEolStreamReader : TextReader, IDisposable
    {
        [DebuggerDisplay("{DebuggerDisplay}")]
        private class MemoryStreamRW : IDisposable
        {
            public MemoryStreamRW(Encoding encoding)
            {
                this.Stream = new();
                this.Reader = new(this.Stream, encoding);
                this.Writer = new(this.Stream, encoding) { AutoFlush = true };
            }

            public MemoryStream Stream { get; }

            public StreamWriter Writer { get; }

            public StreamReader Reader { get; }

            private string DebuggerDisplay => Encoding.UTF8.GetString(this.Stream.ToArray());

            public void Dispose()
            {
                this.Stream.Dispose();
            }
        }

        private readonly MemoryStreamRW outBuffer = new(Encoding.UTF8);
        private readonly StreamReader sourceReader;
        private readonly char[] buffer = new char[1024];
        private int bufferPos = -1;
        private int bufferSize;
        private bool lastCharWasCR;
        private int eolCount;
        private bool forceFlushEol;
        private bool reachedEnd;

        public NoEolStreamReader(Stream stream)
        {
            this.sourceReader = new StreamReader(stream, Encoding.UTF8);
            //Trace.Write("\n\n---------\n\n");
        }

        public override void Close() => this.sourceReader.Close();

        public override int Peek()
        {
            if (this.reachedEnd) return -1;

            var c = this.outBuffer.Reader.Peek();
            if (c < 0)
            {
                this.UpdateOutBuffer();
                c = this.outBuffer.Reader.Peek();
                if (c < 0) return '\n';
            }

            return c;
        }

        public override int Read()
        {
            if (this.reachedEnd) return -1;

            var c = this.outBuffer.Reader.Read();
            if (c < 0)
            {
                this.UpdateOutBuffer();
                c = this.outBuffer.Reader.Read();
                if (c < 0)
                {
                    this.reachedEnd = true;
                    return '\n';
                }
            }

            //Trace.Write((char)c);
            return c;
        }

        private void UpdateOutBuffer()
        {
            this.outBuffer.Stream.SetLength(0);
            this.outBuffer.Reader.DiscardBufferedData();

            var eolPosInBuffer = -1;
            var isLineEmpty = true;
            while (true)
            {
                if (this.bufferPos < 0 || this.bufferPos >= this.bufferSize)
                {
                    this.bufferPos = 0;
                    this.bufferSize = this.sourceReader.Read(this.buffer, 0, this.buffer.Length);
                    if (this.bufferSize == 0) break;
                }

                // If the last char we read was CR ('\r') since we last read
                // the stream, skip the LF (CR-LF = Windows-style line endings)
                if (this.lastCharWasCR && this.buffer[this.bufferPos] == '\n')
                {
                    this.bufferPos++;
                    this.lastCharWasCR = false;
                }

                var startIndex = this.bufferPos;
                var charCount = 0;
                for (var i = startIndex; i < this.bufferSize; i++)
                {
                    charCount++;
                    if (this.buffer[i] == '\n')
                    {
                        eolPosInBuffer = i;
                        break;
                    }
                    else if (this.buffer[i] == '\r')
                    {
                        this.lastCharWasCR = true;
                        eolPosInBuffer = i;
                        break;
                    }
                    else
                    {
                        isLineEmpty = false;
                    }
                }

                var foundEol = eolPosInBuffer >= 0;

                if ((foundEol || this.forceFlushEol) && !isLineEmpty && this.eolCount > 0)
                {
                    this.outBuffer.Writer.Write(new string('\n', this.eolCount));
                    this.eolCount = 0;
                    this.forceFlushEol = false;
                }

                if (!foundEol)
                {
                    this.outBuffer.Writer.Write(this.buffer, startIndex, charCount);
                    this.bufferPos = this.bufferSize;
                    continue;
                }

                this.outBuffer.Writer.Write(this.buffer, startIndex, charCount - 1);
                this.eolCount++;

                this.bufferPos = eolPosInBuffer + 1;
                eolPosInBuffer = -1;

                if (!isLineEmpty) break;
            }

            this.outBuffer.Writer.Flush();
            this.outBuffer.Stream.Position = 0;
            this.forceFlushEol = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.outBuffer.Dispose();
            this.sourceReader.Dispose();
        }
    }
}
