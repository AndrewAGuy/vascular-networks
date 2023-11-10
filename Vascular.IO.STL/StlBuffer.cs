using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.STL
{
    /// <summary>
    /// Can be used for writing meshes in parts.
    /// </summary>
    public class StlBuffer
    {
        /// <summary>
        ///
        /// </summary>
        protected BinaryWriter stream;

        /// <summary>
        ///
        /// </summary>
        protected uint count = 0;

        /// <summary>
        ///
        /// </summary>
        public uint Count => count;

        /// <summary>
        ///
        /// </summary>
        public Stream Stream => stream.BaseStream;

        /// <summary>
        /// Allocates a <see cref="MemoryStream"/> of <paramref name="chunk"/> length if <paramref name="chunk"/> is not 0,
        /// otherwise takes the stream given in <paramref name="underlying"/>.
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="underlying"></param>
        public StlBuffer(int chunk = 1 << 20, Stream? underlying = null)
        {
            stream = chunk != 0
                ? new BinaryWriter(new MemoryStream(chunk))
                : underlying is not null
                    ? new BinaryWriter(underlying)
                    : BinaryWriter.Null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="v"></param>
        protected void Write(Vector3 v)
        {
            float x = (float)v.x, y = (float)v.y, z = (float)v.z;
            stream.Write(x);
            stream.Write(y);
            stream.Write(z);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="n"></param>
        public void Write(Vector3 a, Vector3 b, Vector3 c, Vector3 n)
        {
            Write(n);
            Write(a);
            Write(b);
            Write(c);
            ushort attr = 0;
            stream.Write(attr);
            count++;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public void Write(Vector3 a, Vector3 b, Vector3 c)
        {
            Write(a, b, c, ((b - a) ^ (c - a)).Normalize());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="t"></param>
        public void Write(Triangle t)
        {
            Write(t.A.P, t.B.P, t.C.P, t.N);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="T"></param>
        public void Write(IEnumerable<Triangle> T)
        {
            foreach (var t in T)
            {
                Write(t);
            }
        }

        /// <summary>
        /// Some programs like STL files to be ordered upwards. If so, set <paramref name="zOrder"/> to true.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="zOrder"></param>
        public void Write(Mesh mesh, bool zOrder = false)
        {
            if (zOrder)
            {
                double minZ(Triangle t) => Math.Min(Math.Min(t.A.P.z, t.B.P.z), t.C.P.z);
                double maxZ(Triangle t) => Math.Max(Math.Max(t.A.P.z, t.B.P.z), t.C.P.z);
                Write(mesh.T.OrderBy(minZ).ThenBy(maxZ));
            }
            else
            {
                Write(mesh.T);
            }
        }
    }
}
