using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.STL
{
    public class StlBuffer
    {
        protected BinaryWriter stream;
        protected uint count = 0;

        public uint Count => count;

        public Stream Stream => stream.BaseStream;

        public StlBuffer(int chunk = 1 << 20)
        {
            stream = chunk != 0 ? new BinaryWriter(new MemoryStream(chunk)) : null;
        }

        protected void Write(Vector3 v)
        {
            float x = (float)v.x, y = (float)v.y, z = (float)v.z;
            stream.Write(x);
            stream.Write(y);
            stream.Write(z);
        }

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

        public void Write(Vector3 a, Vector3 b, Vector3 c)
        {
            Write(a, b, c, ((b - a) ^ (c - a)).Normalize());
        }

        public void Write(Triangle t)
        {
            Write(t.A.P, t.B.P, t.C.P, t.N);
        }

        public void Write(IEnumerable<Triangle> T)
        {
            foreach (var t in T)
            {
                Write(t);
            }
        }

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
