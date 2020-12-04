using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.STL
{
    public static class StlReader
    {
        public static Mesh FromFile(string path, Func<Vector3, Vector3> transform = null, bool invertNormals = false)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return FromStream(stream, transform, invertNormals);
        }

        private const int HEADER = 80;
        private const string SOLID = "solid";
        private const string ENDSOLID = "endsolid";
        private const string FACET = "facet";
        private const string NORMAL = "normal";
        private const string ENDFACET = "endfacet";
        private const string OUTER = "outer";
        private const string LOOP = "loop";
        private const string ENDLOOP = "endloop";
        private const string VERTEX = "vertex";

        public static Mesh FromStream(Stream stream, Func<Vector3, Vector3> transform = null, bool invertNormals = false)
        {
            transform ??= new Func<Vector3, Vector3>(v => v);
            var m = new Mesh();
            var sr = new StreamReader(stream, Encoding.ASCII);
            // If this is an ASCII file, we first find a magic string before the model name
            var ch = new char[SOLID.Length];
            sr.Read(ch, 0, SOLID.Length);
            if (string.Equals(new string(ch), SOLID, StringComparison.Ordinal))
            {
                ReadASCII(m, sr, transform, invertNormals);
            }
            else
            {
                stream.Seek(0, SeekOrigin.Begin);
                var br = new BinaryReader(stream);
                // Skip header bytes
                br.ReadBytes(HEADER);
                ReadBinary(m, br, transform, invertNormals);
            }
            return m;
        }

        private static void ReadASCII(Mesh m, StreamReader r, Func<Vector3, Vector3> tf, bool n)
        {
            // Name takes whole line according to standard
            r.ReadLine();
            // Everything else is simply read by token, no enforcement of newlines
            var t = ReadToken(r);
            while (t != null)
            {
                if (string.Equals(t, ENDSOLID, StringComparison.Ordinal))
                {
                    // We only want to read one mesh at present
                    return;
                }
                else if (string.Equals(t, FACET, StringComparison.Ordinal))
                {
                    ReadFacet(m, r, tf, n);
                }
                else
                {
                    throw new FileFormatException($"Invalid ASCII STL token: expected '{ENDSOLID}' or '{FACET}', found '{t}'");
                }
                t = ReadToken(r);
            }
            throw new FileFormatException($"Invalid ASCII STL: file ended before '{ENDSOLID}' token");
        }

        private static void ReadFacet(Mesh m, StreamReader r, Func<Vector3, Vector3> tf, bool n)
        {
            // Normal vector. We overwrite it since we allow arbitrary transforms, but the spec demands it be present.
            var t = ReadToken(r);
            if (!string.Equals(t, NORMAL, StringComparison.Ordinal))
            {
                throw new FileFormatException($"Invalid ASCII STL token: expected '{NORMAL}', found '{t}'");
            }
            t = ReadToken(r);
            if (!double.TryParse(t, out var x))
            {
                throw new FileFormatException($"Invalid ASCII STL: could not parse facet normal x value '{t}'");
            }
            t = ReadToken(r);
            if (!double.TryParse(t, out var y))
            {
                throw new FileFormatException($"Invalid ASCII STL: could not parse facet normal y value '{t}'");
            }
            t = ReadToken(r);
            if (!double.TryParse(t, out var z))
            {
                throw new FileFormatException($"Invalid ASCII STL: could not parse facet normal z value '{t}'");
            }

            // Loop introduction
            t = ReadToken(r);
            if (!string.Equals(t, OUTER, StringComparison.Ordinal))
            {
                throw new FileFormatException($"Invalid ASCII STL token: expected '{OUTER}', found '{t}'");
            }
            t = ReadToken(r);
            if (!string.Equals(t, LOOP, StringComparison.Ordinal))
            {
                throw new FileFormatException($"Invalid ASCII STL token: expected '{LOOP}', found '{t}'");
            }

            // Vertices
            var vtx = new List<Vector3>(3);
            while (true)
            {
                t = ReadToken(r);
                if (!string.Equals(t, VERTEX, StringComparison.Ordinal))
                {
                    break;
                }
                t = ReadToken(r);
                if (!double.TryParse(t, out x))
                {
                    throw new FileFormatException($"Invalid ASCII STL: could not parse vertex x value '{t}'");
                }
                t = ReadToken(r);
                if (!double.TryParse(t, out y))
                {
                    throw new FileFormatException($"Invalid ASCII STL: could not parse vertex y value '{t}'");
                }
                t = ReadToken(r);
                if (!double.TryParse(t, out z))
                {
                    throw new FileFormatException($"Invalid ASCII STL: could not parse vertex z value '{t}'");
                }
                vtx.Add(tf(new Vector3(x, y, z)));
            }

            // We aren't going to support arbitrary loops
            if (vtx.Count < 3)
            {
                throw new FileFormatException($"Invalid ASCII STL: facet with fewer than 3 vertices ({vtx.Count})");
            }
            else if (vtx.Count > 3)
            {
                throw new FileFormatException($"Unsupported ASCII STL: facet with more than 3 vertices ({vtx.Count})");
            }
            if (n)
            {
                m.AddTriangle(vtx[0], vtx[2], vtx[1], ((vtx[2] - vtx[0]) ^ (vtx[1] - vtx[0])).Normalize());
            }
            else
            {
                m.AddTriangle(vtx[0], vtx[1], vtx[2], ((vtx[1] - vtx[0]) ^ (vtx[2] - vtx[0])).Normalize());
            }

            // Loop ending, should have been read already
            if (!string.Equals(t, ENDLOOP, StringComparison.Ordinal))
            {
                throw new FileFormatException($"Invalid ASCII STL token: expected '{VERTEX}' or '{ENDLOOP}', found '{t}'");
            }

            // Facet ending
            t = ReadToken(r);
            if (!string.Equals(t, ENDFACET, StringComparison.Ordinal))
            {
                throw new FileFormatException($"Invalid ASCII STL token: expected '{ENDFACET}', found '{t}'");
            }
        }

        private static string ReadToken(StreamReader r)
        {
            var s = new StringBuilder();
            // Skip whitespace
            while (true)
            {
                var i = r.Read();
                if (i == -1)
                {
                    return string.Empty;
                }
                var c = (char)i;
                if (!char.IsWhiteSpace(c))
                {
                    s.Append(c);
                    break;
                }
            }
            // Advance up to whitespace
            while (true)
            {
                var i = r.Read();
                if (i == -1)
                {
                    break;
                }
                var c = (char)i;
                if (char.IsWhiteSpace(c))
                {
                    break;
                }
                s.Append(c);
            }
            return s.ToString();
        }

        private static void ReadBinary(Mesh m, BinaryReader r, Func<Vector3, Vector3> t, bool n)
        {
            var e = r.ReadUInt32();
            for (uint i = 0; i < e; ++i)
            {
                SkipBinaryVector3(r);
                var a = t(ReadBinaryVector3(r));
                var b = t(ReadBinaryVector3(r));
                var c = t(ReadBinaryVector3(r));
                r.ReadUInt16();
                if (n)
                {
                    m.AddTriangle(a, c, b, ((c - a) ^ (b - a)).Normalize());
                }
                else
                {
                    m.AddTriangle(a, b, c, ((b - a) ^ (c - a)).Normalize());
                }
            }
        }

        private static void SkipBinaryVector3(BinaryReader r)
        {
            r.ReadSingle();
            r.ReadSingle();
            r.ReadSingle();
        }

        private static Vector3 ReadBinaryVector3(BinaryReader r)
        {
            var x = r.ReadSingle();
            var y = r.ReadSingle();
            var z = r.ReadSingle();
            return new Vector3(x, y, z);
        }
    }
}
