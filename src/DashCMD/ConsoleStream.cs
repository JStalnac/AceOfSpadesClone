using System.IO;

namespace Dash.CMD
{
    internal class ConsoleStream : Stream
    {
        public event ConsoleStreamWrite OnConsoleWrite;
        public event ConsoleStreamRead OnConsoleRead;
        public event ConsoleStreamSeek OnConsoleSeek;

        public delegate void ConsoleStreamWrite(byte[] buffer, int offset, int count);
        public delegate void ConsoleStreamRead(int value, int offset, int count);
        public delegate void ConsoleStreamSeek(long newPos);

        private Stream inner;

        public ConsoleStream(Stream inner)
        {
            this.inner = inner;
        }

        public override bool CanRead
        {
            get { return inner.CanRead; }
        }

        public override bool CanSeek
        {
            get { return inner.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return inner.CanWrite; }
        }

        public override void Flush()
        {
            inner.Flush();
        }

        public override long Length
        {
            get { return inner.Length; }
        }

        public override long Position
        {
            get { return inner.Position; }
            set { inner.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int value = inner.Read(buffer, offset, count);

            if (OnConsoleRead != null)
                OnConsoleRead(value, offset, count);

            return value;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long value = inner.Seek(offset, origin);

            if (OnConsoleSeek != null)
                OnConsoleSeek(value);

            return value;
        }

        public override void SetLength(long value)
        {
            inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
            Flush();

            if (OnConsoleWrite != null)
                OnConsoleWrite(buffer, offset, count);
        }
    }
}