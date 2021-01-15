using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Geometry.Triangulation
{
    [DataContract]
    public class Mesh : IAxialBoundable, IEnumerable<Triangle>, IEnumerable<Edge>, IEnumerable<Vertex>
    {
        [DataMember]
        public LinkedList<Triangle> T = new LinkedList<Triangle>();
        [DataMember]
        public Dictionary<Edge, Edge> E = new Dictionary<Edge, Edge>();
        [DataMember]
        public Dictionary<Vector3, Vertex> V = new Dictionary<Vector3, Vertex>();

        public Vertex AddVertex(Vector3 p)
        {
            if (!V.TryGetValue(p, out var v))
            {
                v = new Vertex(p);
                V[p] = v;
            }
            return v;
        }

        public Vertex GetVertex(Vector3 p)
        {
            return V.TryGetValue(p, out var v) ? v : null;
        }

        public void MoveVertex(Vertex v, Vector3 p)
        {
            V.Remove(v.P);
            v.P = p;
            V[p] = v;
        }

        public Edge AddEdge(Vertex a, Vertex b)
        {
            var vp = new Edge(a, b);
            if (E.TryGetValue(vp, out var e))
            {
                return e;
            }
            vp.T = new LinkedList<Triangle>();
            E[vp] = vp;
            return vp;
        }

        public void CollapseEdge(Edge e, Vector3 p)
        {
            // Find existing triangles and the contour to rebuild
            var c = new List<Edge>();
            var r = new HashSet<Triangle>();
            foreach (var t in e.S.T)
            {
                r.Add(t);
                if (!e.T.Contains(t))
                {
                    c.Add(t.Opposite(e.S).CorrectWindingIn(t));
                }
            }
            foreach (var t in e.E.T)
            {
                r.Add(t);
                if (!e.T.Contains(t))
                {
                    c.Add(t.Opposite(e.E).CorrectWindingIn(t));
                }
            }
            // Remove the triangles
            foreach (var t in r)
            {
                RemoveTriangle(t);
            }
            // Rebuild the contour
            foreach (var x in c)
            {
                AddTriangle(p, x.S.P, x.E.P);
            }
        }

        public void Merge(Mesh other)
        {
            foreach (var t in other.T)
            {
                AddTriangle(t.A.P, t.B.P, t.C.P);
            }
        }

        public Triangle AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            return AddTriangle(a, b, c, ((b - a) ^ (c - a)).Normalize());
        }

        public Triangle AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 n)
        {
            var A = AddVertex(a);
            var B = AddVertex(b);
            var C = AddVertex(c);
            var AB = AddEdge(A, B);
            var BC = AddEdge(B, C);
            var CA = AddEdge(C, A);
            if (!A.E.Contains(AB))
            {
                A.E.AddLast(AB);
            }
            if (!B.E.Contains(AB))
            {
                B.E.AddLast(AB);
            }
            if (!B.E.Contains(BC))
            {
                B.E.AddLast(BC);
            }
            if (!C.E.Contains(BC))
            {
                C.E.AddLast(BC);
            }
            if (!C.E.Contains(CA))
            {
                C.E.AddLast(CA);
            }
            if (!A.E.Contains(CA))
            {
                A.E.AddLast(CA);
            }
            var NT = new Triangle(A, B, C, AB, BC, CA, n);
            A.T.AddLast(NT);
            B.T.AddLast(NT);
            C.T.AddLast(NT);
            AB.T.AddLast(NT);
            BC.T.AddLast(NT);
            CA.T.AddLast(NT);
            NT.Node = T.AddLast(NT);
            return NT;
        }

        public void RemoveTriangle(Triangle t)
        {
            T.Remove(t.Node);
            // Remove references held by edges. Do these edges still exist?
            t.AB.T.Remove(t);
            if (t.AB.T.Count == 0)
            {
                t.AB.S.E.Remove(t.AB);
                t.AB.E.E.Remove(t.AB);
                E.Remove(t.AB);
            }
            t.BC.T.Remove(t);
            if (t.BC.T.Count == 0)
            {
                t.BC.S.E.Remove(t.BC);
                t.BC.E.E.Remove(t.BC);
                E.Remove(t.BC);
            }
            t.CA.T.Remove(t);
            if (t.CA.T.Count == 0)
            {
                t.CA.S.E.Remove(t.CA);
                t.CA.E.E.Remove(t.CA);
                E.Remove(t.CA);
            }
            // If all triangles sharing an edge connected to the vertex disappear, the edge will have been removed.
            // We can therefore pre-test on edges, then remove triangles if they survive.
            if (t.A.E.Count == 0)
            {
                V.Remove(t.A.P);
            }
            else
            {
                t.A.T.Remove(t);
            }
            if (t.B.E.Count == 0)
            {
                V.Remove(t.B.P);
            }
            else
            {
                t.B.T.Remove(t);
            }
            if (t.C.E.Count == 0)
            {
                V.Remove(t.C.P);
            }
            else
            {
                t.C.T.Remove(t);
            }
        }

        public void RemoveVertex(Vertex v)
        {
            var t = v.T.ToList();
            foreach (var tr in t)
            {
                RemoveTriangle(tr);
            }
        }

        public int EulerCharacteristic =>
                // 2(1 - g) = v - e + f = x
                V.Count - E.Count + T.Count;

        public double Genus => 1 - 0.5 * this.EulerCharacteristic;

        public bool VerifyEdgeCounts
        {
            get
            {
                foreach (var e in E.Values)
                {
                    if (e.T.Count > 2 || e.T.Count == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool IsClosed
        {
            get
            {
                foreach (var e in E.Values)
                {
                    if (e.T.Count != 2)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool HasBoundary
        {
            get
            {
                foreach (var e in E.Values)
                {
                    if (e.T.Count == 1)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsOneSided
        {
            get
            {
                foreach (var e in E.Values)
                {
                    if (e.T.Count == 2 && !e.CheckNormalConsistency())
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public AxialBounds GetAxialBounds()
        {
            var e = V.GetEnumerator();
            if (!e.MoveNext())
            {
                throw new PhysicalValueException("Empty mesh has no bounds.");
            }
            var b = new AxialBounds(e.Current.Key);
            while (e.MoveNext())
            {
                b.Append(e.Current.Key);
            }
            return b;
        }

        public Mesh Copy()
        {
            var m = new Mesh();
            foreach (var t in T)
            {
                m.AddTriangle(t.A.P, t.B.P, t.C.P, t.N);
            }
            return m;
        }

        public Mesh Transform(Matrix3 A, Vector3 v)
        {
            var m = new Mesh();
            foreach (var t in T)
            {
                m.AddTriangle(A * t.A.P + v, A * t.B.P + v, A * t.C.P + v);
            }
            return m;
        }

        public Mesh Transform(Func<Vector3, Vector3> f)
        {
            var m = new Mesh();
            foreach (var t in T)
            {
                m.AddTriangle(f(t.A.P), f(t.B.P), f(t.C.P));
            }
            return m;
        }

        public void Extrema(Vector3 dir, out Vertex max, out Vertex min)
        {
            var e = V.GetEnumerator();
            if (!e.MoveNext())
            {
                throw new PhysicalValueException("Empty mesh has no extrema.");
            }
            var vmax = dir * e.Current.Key;
            var vmin = vmax;
            max = min = e.Current.Value;
            while (e.MoveNext())
            {
                var vcur = dir * e.Current.Key;
                if (vcur > vmax)
                {
                    vmax = vcur;
                    max = e.Current.Value;
                }
                else if (vcur < vmin)
                {
                    vmin = vcur;
                    min = e.Current.Value;
                }
            }
        }

        public Mesh ReverseNormals()
        {
            var m = new Mesh();
            foreach (var t in T)
            {
                m.AddTriangle(t.A.P, t.C.P, t.B.P);
            }
            return m;
        }

        public void SetGroupNormals()
        {
            foreach (var v in V.Values)
            {
                v.SetGroupNormal();
            }
        }

        public void SetGroupNormals(Func<Triangle, Vertex, double> weighting)
        {
            foreach (var v in V.Values)
            {
                v.SetGroupNormal(weighting);
            }
        }

        IEnumerator<Triangle> IEnumerable<Triangle>.GetEnumerator()
        {
            return T.GetEnumerator();
        }

        IEnumerator<Edge> IEnumerable<Edge>.GetEnumerator()
        {
            return E.Values.GetEnumerator();
        }

        IEnumerator<Vertex> IEnumerable<Vertex>.GetEnumerator()
        {
            return V.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return T.GetEnumerator();
        }

        public Edge GetEdge(Vector3 a, Vector3 b)
        {
            return
                GetVertex(a) is not Vertex A ||
                GetVertex(b) is not Vertex B ||
                !E.TryGetValue(new Edge(A, B), out var e)
                ? null : e;
        }

        public Triangle GetTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            if (GetVertex(a) is not Vertex A ||
                GetVertex(b) is not Vertex B ||
                GetVertex(c) is not Vertex C)
            {
                return null;
            }
            var ab = new Edge(A, B);
            if (!E.TryGetValue(ab, out var AB))
            {
                return null;
            }
            foreach (var t in AB.T)
            {
                if (t.Contains(A) && t.Contains(B) && t.Contains(C))
                {
                    return t;
                }
            }
            return null;
        }
    }
}
