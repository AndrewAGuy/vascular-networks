﻿using System;
using System.IO;
using System.Text;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.STL
{
    /// <summary>
    /// Can accept other buffers, and writes headers to the stream as well.
    /// </summary>
    public class StlWriter : StlBuffer, IDisposable
    {
        /// <summary>
        /// Writes triangles in binary STL format to <paramref name="stream"/>. Writes an 80 byte header from <paramref name="header"/>,
        /// padding with <paramref name="padChar"/> if needed.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="header"></param>
        /// <param name="padChar"></param>
        public StlWriter(Stream stream, string header = "", char padChar = ' ') : base(0, stream)
        {
            Begin(header, padChar);
        }

        /// <summary>
        /// Writes all triangles in a mesh to the file. For options, see <see cref="StlWriter(Stream, string, char)"/>.
        /// Optionally order the mesh by height using <paramref name="zOrder"/>, see <see cref="StlBuffer.Write(Mesh, int)"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mesh"></param>
        /// <param name="header"></param>
        /// <param name="padChar"></param>
        /// <param name="zOrder"></param>
        public static void ToFile(string path, Mesh mesh, string header = "", char padChar = ' ', int zOrder = 0)
        {
            using var writer = new StlWriter(new FileStream(path, FileMode.Create, FileAccess.Write), header, padChar);
            writer.Write(mesh, zOrder);
        }

        private const int HEADER_SIZE = 80;
        private const string SOLID = "solid";

        private void Begin(string header, char padChar = ' ')
        {
            if (header.StartsWith(SOLID, StringComparison.Ordinal))
            {
                throw new FileFormatException($"Cannot start binary STL file with ASCII keyword: '{SOLID}'");
            }
            var formatted =
                header.Length < HEADER_SIZE
                ? header.PadRight(HEADER_SIZE, padChar)
                : header.Length > HEADER_SIZE
                ? header.Substring(0, HEADER_SIZE)
                : header;
            var bytes = Encoding.UTF8.GetBytes(formatted);
            stream.Write(bytes, 0, HEADER_SIZE);
            stream.Write(0);
        }

        private bool disposed = false;

        /// <summary>
        ///
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="disposing"></param>
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

        /// <summary>
        ///
        /// </summary>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        public void Accept(StlBuffer buffer)
        {
            buffer.Stream.Seek(0, SeekOrigin.Begin);
            buffer.Stream.CopyTo(stream.BaseStream);
            count += buffer.Count;
        }
    }
}
