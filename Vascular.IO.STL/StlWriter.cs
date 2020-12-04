using System;
using System.IO;
using System.Text;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.STL
{
    public class StlWriter : StlBuffer, IDisposable
    {
        public StlWriter(FileStream stream)
        {
            this.stream = new BinaryWriter(stream);
            Begin();
        }

        public StlWriter(FileStream stream, string header)
        {
            this.stream = new BinaryWriter(stream);
            Begin(header);
        }

        public static void ToFile(string path, Mesh mesh, string header)
        {
            using var writer = new StlWriter(new FileStream(path, FileMode.Create, FileAccess.Write), header ?? "");
            writer.Write(mesh);
        }

        private const int HEADER_SIZE = 80;

        private void Begin()
        {
            byte zero = 0;
            for (var i = 0; i < HEADER_SIZE; ++i)
            {
                stream.Write(zero);
            }
            stream.Write(0);
        }

        private void Begin(string header)
        {
            var formatted =
                header.Length < HEADER_SIZE
                ? header.PadRight(HEADER_SIZE, ' ')
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
