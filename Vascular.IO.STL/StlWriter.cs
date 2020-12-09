using System;
using System.IO;
using System.Text;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.STL
{
    public class StlWriter : StlBuffer, IDisposable
    {
        public StlWriter(Stream stream, string header = null, char padChar = ' ') : base(0)
        {
            this.stream = new BinaryWriter(stream);
            Begin(header ?? string.Empty, padChar);
        }

        public static void ToFile(string path, Mesh mesh, string header = null, char padChar = ' ', bool zOrder = false)
        {
            using var writer = new StlWriter(new FileStream(path, FileMode.Create, FileAccess.Write), header, padChar);
            writer.Write(mesh, zOrder);
        }

        private const int HEADER_SIZE = 80;

        private void Begin(string header, char padChar = ' ')
        {
            var formatted =
                header.Length < HEADER_SIZE
                ? header.PadRight(HEADER_SIZE, padChar)
                : header.Length > HEADER_SIZE
                ? header.Substring(0, HEADER_SIZE)
                : header;
            var bytes = Encoding.ASCII.GetBytes(formatted);
            stream.Write(bytes, 0, HEADER_SIZE);
            stream.Write(0);
        }

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                End();
                if (disposing)
                {
                    stream.Dispose();
                }
                disposed = true;
            }
        }

        ~StlWriter()
        {
            Dispose(false);
        }

        private void End()
        {
            stream.Seek(HEADER_SIZE, SeekOrigin.Begin);
            stream.Write(count);
            stream.Seek(0, SeekOrigin.End);
        }

        public void Accept(StlBuffer buffer)
        {
            buffer.Stream.Seek(0, SeekOrigin.Begin);
            buffer.Stream.CopyTo(stream.BaseStream);
            count += buffer.Count;
        }
    }
}
