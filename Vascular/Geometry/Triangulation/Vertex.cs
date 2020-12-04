using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Geometry.Triangulation
{
    [Serializable]
    public class Vertex
    {
        public Vertex(Vector3 p)
        {
            P = p;
        }

        public Vector3 P;

        public LinkedList<Edge> E = new LinkedList<Edge>();
        public LinkedList<Triangle> T = new LinkedList<Triangle>();

        public Vector3 N;

        public Vector3 SetGroupNormal(Func<Triangle, Vertex, double> weighting)
        {
            var v = new Vector3();
            var w = 0.0;
            foreach (var t in T)
            {
                var wt = weighting(t, this);
                v += t.N * wt;
                w += wt;
            }
            return N = (v / w).Normalize();
        }

        public static double AngleWeighting(Triangle t, Vertex v)
        {
            double a, b, c;
            if (v == t.A)
            {
                a = t.AB.Length;
                b = t.CA.Length;
                c = t.BC.Length;
            }
            else if (v == t.B)
            {
                a = t.AB.Length;
                b = t.BC.Length;
                c = t.CA.Length;
            }
            else if (v == t.C)
            {
                a = t.BC.Length;
                b = t.CA.Length;
                c = t.AB.Length;
            }
            else
            {
                return 0;
            }
            var x = (a * a + b * b - c * c) / (2 * a * b);
            x = x.Clamp(-1, 1);
            return Math.Acos(x);
        }

        public Vector3 SetGroupNormal()
        {
            return SetGroupNormal(AngleWeighting);
        }

        public List<Vertex> Fan
        {
            get
            {
                var currentTriangle = T.First.Value;
                var outerEdge = currentTriangle.Opposite(this).CorrectWindingIn(currentTriangle);
                var path = new List<Vertex>();
                var first = outerEdge.S;
                path.Add(first);
                path.Add(outerEdge.E);
                while (true)
                {
                    var secondLast = path[^2];
                    var edgeInner = currentTriangle.Opposite(secondLast);
                    var nextTriangle = edgeInner.Other(currentTriangle);
                    outerEdge = nextTriangle.Opposite(this).CorrectWindingIn(nextTriangle);
                    if (outerEdge.E == first)
                    {
                        break;
                    }
                    path.Add(outerEdge.E);
                    currentTriangle = nextTriangle;
                }
                return path;
            }
        }

        public List<Vertex> UnorderedFan
        {
            get
            {
                var vertices = new List<Vertex>(E.Count);
                foreach (var e in E)
                {
                    vertices.Add(e.Other(this));
                }
                return vertices;
            }
        }

        public bool IsConnected(Vertex other)
        {
            return E.Any(e => e.Other(this) == other);
        }

        public bool IsInterior => E.All(e => e.T.Count == 2);
    }
}
