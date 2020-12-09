using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.Triangulation
{
    public class Decimator
    {
        public Func<Vertex, double> MeanPlaneWeight { get; set; } = v => 1;
        public double MeanPlaneTolerance { get; set; } = 1.0e-12;
        public double MeanPlaneRatioSquared { get; set; } = 4;
        public bool TestNonManifold { get; set; } = false;
        public bool ThrowIfNonManifold { get; set; } = true;
        public double RidgeDihedralAngleCosine { get; set; } = Math.Cos(Math.PI / 4.0);
        public double MinDihedralAngleCosine { get; set; } = 0;
        public double SmallEdgeLengthFraction { get; set; } = 0.25;
        public double MinLoopLengthFraction { get; set; } = 0.0625;
        public double MinLoopAspectRatio { get; set; } = 0.125;
        public bool DecimateBoundary { get; set; } = false;
        public Func<double, double, double> TotalErrorUpdate { get; set; } = (eOld, eNew) => eOld + eNew;
        public bool SortCandidates { get; set; } = true;
        public bool RemoveVertices { get; set; } = true;
        public bool DecimateRidges { get; set; } = true;
        public bool CollapseEdges { get; set; } = false;
        public bool PerTriangleCost { get; set; } = true;
        public bool AccumulateCost { get; set; } = true;

        private record Triple(Vertex a, Vertex b, Vertex c);

        //public class IterationCompleteEventArgs : EventArgs
        //{
        //    public int NumTriangles { get; init; }
        //    public int NumVertices { get; init; }
        //    public int NumEdges { get; init; }
        //    public double MaxError { get; init; }
        //    public int NumCandidateVertices { get; init; }
        //    public int NumCandidateEdges { get; init; }
        //}
        //public event EventHandler<IterationCompleteEventArgs> IterationComplete;

        private double meshError = 0.0;
        private double targetError;
        private Dictionary<Triangle, double> meshErrors;
        private HashSet<Vertex> candidateVertices;
        private HashSet<Edge> candidateEdges;
        private Mesh mesh;

        private bool TestEdgeCollapse(Edge edge)
        {
            var fan = edge.S.UnorderedFan;
            var joint = 0;
            foreach (var v in edge.E.UnorderedFan)
            {
                if (fan.Contains(v))
                {
                    ++joint;
                }
            }
            return joint == 2;
        }

        

        

        public void DecimateDistance(Mesh mesh, double targetValue)
        {
            var smallEdgeLength = targetValue * this.SmallEdgeLengthFraction;
            var smallLoopLength = targetValue * this.MinLoopLengthFraction;
            var totalError = 0.0;
            while (true)
            {
                var (simple, ridges) = Classify(mesh, smallEdgeLength);
                if (this.SortCandidates)
                {
                    simple.Sort();
                    ridges.Sort();
                }
                var visited = new HashSet<Vertex>(mesh.V.Count);
                var newError = 0.0;
                var remainingError = targetValue - totalError;
                foreach (var s in simple)
                {
                    if (s.D < remainingError && TryRemove(s, mesh, visited, smallLoopLength))
                    {
                        newError = Math.Max(newError, s.D);
                    }
                }

                if (this.DecimateRidges)
                {
                    foreach (var r in ridges)
                    {
                        if (r.D < remainingError && TryRemove(r, mesh, visited, smallLoopLength))
                        {
                            newError = Math.Max(newError, r.D);
                        }
                    }
                }

                totalError = this.TotalErrorUpdate(totalError, newError);
                if (visited.Count == 0)
                {
                    return;
                }
            }
        }

        private bool TestRemesh(Mesh m, Vertex v, List<(Vertex a, Vertex b, Vertex c)> T, List<Vertex> f)
        {
            var fE = new HashSet<Edge>(f.Count) { new Edge(f[^1], f[0]) };
            for (var i = 0; i < f.Count - 1; ++i)
            {
                fE.Add(new Edge(f[i], f[i + 1]));
            }
            var eD = new Dictionary<Edge, Vector3>(f.Count * 2);
            foreach (var (a, b, c) in T)
            {
                if (a.TriangleExists(b, c))
                {
                    return false;
                }
                var ab = new Edge(a, b);
                var bc = new Edge(b, c);
                var ca = new Edge(c, a);
                var N = ((b.P - a.P) ^ (c.P - a.P)).Normalize();
                if (!TestRemeshEdge(m, fE, eD, ab, N, v) ||
                    !TestRemeshEdge(m, fE, eD, bc, N, v) ||
                    !TestRemeshEdge(m, fE, eD, ca, N, v))
                {
                    return false;
                }
            }
            return true;
        }

        private bool TestRemeshEdge(Mesh m, HashSet<Edge> fE, Dictionary<Edge, Vector3> eN, Edge e, Vector3 n, Vertex v)
        {
            if (!fE.Contains(e) && m.E.ContainsKey(e))
            {
                return false;
            }
            if (m.E.TryGetValue(e, out var E))
            {
                var t = E.T.Where(tr => !tr.Contains(v)).ToList();
                if (t.Count == 0 || t[0].N * n < this.MinDihedralAngleCosine)
                {
                    return false;
                }
            }
            else
            {
                if (eN.TryGetValue(e, out var N))
                {
                    if (n * N < this.MinDihedralAngleCosine)
                    {
                        return false;
                    }
                }
                else
                {
                    eN[e] = n;
                }
            }
            return true;
        }

        private bool TryRemove(SimpleData s, Mesh m, HashSet<Vertex> h, double seL)
        {
            if (h.Contains(s.V))
            {
                return false;
            }
            var fan = s.V.Fan;

            if (fan.Count < 3)
            {
                return false;
            }
            var tris = OptimalLoopSplit(fan, s.P, seL);
            if (tris == null || tris.Count != fan.Count - 2)
            {
                return false;
            }
            //var fanEdges = new HashSet<Edge>(fan.Count) { new Edge(fan[^1], fan[0]) };
            //for (var i = 0; i < fan.Count - 1; ++i)
            //{
            //    fanEdges.Add(new Edge(fan[i], fan[i + 1]));
            //}
            //foreach (var (a, b, c) in tris)
            //{
            //    if (a.TriangleExists(b, c))
            //    {
            //        return false;
            //    }
            //    var ab = new Edge(a, b);
            //    var bc = new Edge(b, c);
            //    var ca = new Edge(c, a);


            //    if (!fanEdges.Contains(ab) && m.E.ContainsKey(ab) ||
            //        !fanEdges.Contains(bc) && m.E.ContainsKey(bc) ||
            //        !fanEdges.Contains(ca) && m.E.ContainsKey(ca))
            //    {
            //        return false;
            //    }
            //}
            if (!TestRemesh(m, s.V, tris, fan))
            {
                return false;
            }
            m.RemoveVertex(s.V);
            foreach (var (a, b, c) in tris)
            {
                m.AddTriangle(a.P, b.P, c.P);
            }

            foreach (var v in fan)
            {
                h.Add(v);
            }
            h.Add(s.V);
            return true;
        }

        private bool TryRemove(RidgeData r, Mesh m, HashSet<Vertex> h, double seL)
        {
            if (h.Contains(r.V))
            {
                return false;
            }

            List<(Vertex a, Vertex b, Vertex c)> tris;
            List<Vertex> loop;
            if (!r.E)
            {
                var (fan, a, b) = GetRidgeLoops(r, seL);
                loop = fan;
                if (a == null)
                {
                    return false;
                }
                var A = OptimalLoopSplit(a, r.P, seL);
                var B = OptimalLoopSplit(b, r.P, seL);
                if (A == null || B == null)
                {
                    return false;
                }
                tris = A;
                tris.AddRange(B);
                if (tris.Count != loop.Count - 2)
                {
                    return false;
                }
            }
            else
            {
                loop = GetBoundaryLoop(r.V);
                if (loop == null)
                {
                    return false;
                }
                tris = OptimalLoopSplit(loop, r.P, seL);
                if (tris == null)
                {
                    return false;
                }
            }

            if (!TestRemesh(m, r.V, tris, loop))
            {
                return false;
            }
            m.RemoveVertex(r.V);
            foreach (var (a, b, c) in tris)
            {
                m.AddTriangle(a.P, b.P, c.P);
            }

            foreach (var v in loop)
            {
                h.Add(v);
            }
            h.Add(r.V);
            return true;
        }

        private (List<Vertex> f, List<Vertex> a, List<Vertex> b) GetRidgeLoops(RidgeData r, double seL)
        {
            var fan = r.V.Fan;
            if (fan.Count < 3)
            {
                return (fan, null, null);
            }
            var IA = fan.IndexOf(r.A);
            var IB = fan.IndexOf(r.B);
            if (IA < 0 || IB < 0)
            {
                return (fan, null, null);
            }
            var (a, b, c, ok) = Split(fan, Math.Min(IA, IB), Math.Max(IA, IB), r.P, seL);
            return ok && c > this.MinLoopAspectRatio
                 ? (fan, a, b) : (fan, null, null);
        }

        private static List<Vertex> GetBoundaryLoop(Vertex v)
        {
            // Find which vertex is in the correct order, then wind around from there
            var e = v.E.Where(e => e.T.Count == 1).ToList();
            if (e.Count != 2 || v.T.Count < 2)
            {
                return null;
            }
            var ew0 = e[0].CorrectWindingIn(e[0].T.First.Value);
            var ew1 = e[1].CorrectWindingIn(e[1].T.First.Value);
            var b0 = ew0.S == v;
            var b1 = ew1.S == v;
            var P = new List<Vertex>(v.E.Count);
            Triangle TC = null;
            if (b0 && !b1)
            {
                P.Add(ew0.E);
                TC = e[0].T.First.Value;
            }
            else if (b1 && !b0)
            {
                P.Add(ew1.E);
                TC = e[1].T.First.Value;
            }
            else
            {
                return null;
            }

            // Now copied from vertex fan method
            // Get opposite edge in triangle, then wind it (it will point towards v)
            while (true)
            {
                var EO = TC.Opposite(P[^1]);
                if (EO.T.Count == 1)
                {
                    P.Add(EO.CorrectWindingIn(TC).S);
                    return P;
                }
                var TN = EO.Other(TC);
                P.Add(EO.CorrectWindingIn(TN).E);
                TC = TN;
            }
        }

        private List<(Vertex a, Vertex b, Vertex c)> OptimalLoopSplit(List<Vertex> loop, Plane3 mp, double seL)
        {
            // Pick best by aspect ratio, ensuring that at least 3 points in each
            // Consider splits across first node as well
            if (loop.Count == 3)
            {
                return new List<(Vertex a, Vertex b, Vertex c)>()
                {
                    (loop[0], loop[1], loop[2])
                };
            }
            List<Vertex> A = null, B = null;
            var C = 0.0;
            for (var i = 0; i < loop.Count - 2; ++i)
            {
                for (var j = i + 2; j < loop.Count - 1; ++j)
                {
                    var (a, b, c, ok) = Split(loop, i, j, mp, seL);
                    if (ok && c > C && c > this.MinLoopAspectRatio)
                    {
                        (A, B, C) = (a, b, c);
                    }
                }
            }
            if (C == 0.0)
            {
                return null;
            }
            var RA = OptimalLoopSplit(A, mp, seL);
            if (RA == null)
            {
                return null;
            }
            var RB = OptimalLoopSplit(B, mp, seL);
            if (RB == null)
            {
                return null;
            }
            RA.AddRange(RB);
            return RA;
        }

        private static (List<Vertex> a, List<Vertex> b, double c, bool ok) Split(List<Vertex> V, int p, int q, Plane3 mp, double seL)
        {
            var a = new List<Vertex>(V.Count);
            var b = new List<Vertex>(V.Count);
            for (var i = p; i <= q; ++i)
            {
                a.Add(V[i]);
            }
            for (var i = q; i < V.Count; ++i)
            {
                b.Add(V[i]);
            }
            for (var i = 0; i <= p; ++i)
            {
                b.Add(V[i]);
            }

            var P = V[p];
            var Q = V[q];
            var d = P.P - Q.P;
            var L = d.Length;
            if (L < seL || a.Count < 3 || b.Count < 3)
            {
                return (null, null, 0, false);
            }
            var pn = new Vector3(mp.x, mp.y, mp.z);
            var sd = (pn ^ d).Normalize();
            var sp = new Plane3(sd, sd * P.P);

            var adist = sp.Distance(a[1].P);
            for (var i = 2; i < a.Count - 1; ++i)
            {
                var idist = sp.Distance(a[i].P);
                if (Math.Sign(adist) != Math.Sign(idist))
                {
                    return (null, null, 0, false);
                }
                if (Math.Abs(idist) < Math.Abs(adist))
                {
                    adist = idist;
                }
            }

            var bdist = sp.Distance(b[1].P);
            for (var i = 2; i < b.Count - 1; ++i)
            {
                var idist = sp.Distance(b[i].P);
                if (Math.Sign(bdist) != Math.Sign(idist))
                {
                    return (null, null, 0, false);
                }
                if (Math.Abs(idist) < Math.Abs(bdist))
                {
                    bdist = idist;
                }
            }

            return (a, b, Math.Min(Math.Abs(adist), Math.Abs(bdist)) / L, true);
        }

        private struct RidgeData : IEquatable<RidgeData>, IComparable<RidgeData>
        {
            public Vertex V;
            public double D;
            public Vertex A;
            public Vertex B;
            public bool E;
            public Plane3 P;
            public int CompareTo(RidgeData o)
            {
                return D.CompareTo(o.D);
            }
            public bool Equals(RidgeData o)
            {
                return o.V == V;
            }
            public override bool Equals(object obj)
            {
                return obj is RidgeData o && Equals(o);
            }
            public override int GetHashCode()
            {
                return V.GetHashCode();
            }
        }

        private struct SimpleData : IEquatable<SimpleData>, IComparable<SimpleData>
        {
            public Vertex V;
            public double D;
            public Plane3 P;
            public int CompareTo(SimpleData o)
            {
                return D.CompareTo(o.D);
            }
            public bool Equals(SimpleData o)
            {
                return o.V == V;
            }
            public override bool Equals(object obj)
            {
                return obj is SimpleData o && Equals(o);
            }
            public override int GetHashCode()
            {
                return V.GetHashCode();
            }
        }

        private struct CollapseData : IEquatable<CollapseData>, IComparable<CollapseData>
        {
            public Vertex K;
            public Vertex R;
            public double D;
            public int CompareTo(CollapseData o)
            {
                return D.CompareTo(o.D);
            }
            public bool Equals(CollapseData o)
            {
                return K == o.K && R == o.R;
            }
            public override bool Equals(object obj)
            {
                return obj is CollapseData o && Equals(o);
            }
            public override int GetHashCode()
            {
                return HashCode.Combine(K, R);
            }
        }

        private (List<SimpleData> simple, List<RidgeData> ridges) Classify(Mesh mesh, double smallEdgeLength)
        {
            var simple = new List<SimpleData>(mesh.V.Count);
            var ridges = new List<RidgeData>(mesh.V.Count);
            foreach (var v in mesh.V.Values)
            {
                if (MeanPlane(v) is not Plane3 p)
                {
                    continue;
                }
                if (!v.IsInterior)
                {
                    if (!this.DecimateBoundary)
                    {
                        continue;
                    }
                    var be = v.E.Where(e => e.T.Count == 2).ToList();
                    if (be.Count != 2)
                    {
                        if (this.ThrowIfNonManifold)
                        {
                            throw new TopologyException("Non-manifold node detected");
                        }
                        continue;
                    }
                    var a = be[0].Other(v);
                    var b = be[1].Other(v);
                    ridges.Add(new RidgeData()
                    {
                        V = v,
                        A = a,
                        B = b,
                        D = LinearAlgebra.DistanceToLine(a.P, b.P, v.P),
                        E = true,
                        P = p
                    });
                    continue;
                }

                if (this.TestNonManifold) // We know that all edges have 2 triangles, so should be fine getting a fan
                {
                    var fan = v.Fan;
                    if (v.E.Any(e => !fan.Contains(e.Other(v))))
                    {
                        if (this.ThrowIfNonManifold)
                        {
                            throw new TopologyException("Non-manifold node detected");
                        }
                        continue;
                    }
                }

                var nFE = 0;
                Vertex A = null, B = null;
                foreach (var e in v.E)
                {
                    if (e.Length < smallEdgeLength) // Vertex is now candidate based on mean-plane
                    {
                        nFE = 0;
                        break;
                    }
                    var nDot = e.T.First.Value.N * e.T.Last.Value.N;
                    if (nDot < this.RidgeDihedralAngleCosine)
                    {
                        switch (nFE)
                        {
                            case 0: A = e.Other(v); break;
                            case 1: B = e.Other(v); break;
                        }
                        nFE++;
                    }
                }

                if (nFE < 2)
                {
                    simple.Add(new SimpleData()
                    {
                        V = v,
                        P = p,
                        D = Math.Abs(p.Distance(v.P))
                    });
                }
                else if (nFE == 2 && A != B)
                {
                    ridges.Add(new RidgeData()
                    {
                        A = A,
                        B = B,
                        V = v,
                        D = LinearAlgebra.DistanceToLine(A.P, B.P, v.P),
                        E = false,
                        P = p
                    });
                }
            }
            return (simple, ridges);
        }

        private Plane3 MeanPlane(Vertex V)
        {
            var W = 0.0;
            var m = new Vector3();
            var S = new Matrix3(0);
            foreach (var e in V.E)
            {
                var v = e.Other(V);
                var w = this.MeanPlaneWeight(v);
                W += w;
                m += w * v.P;
                S += w * Matrix3.OuterProduct(v.P);
            }
            m /= W;
            S = S / W - Matrix3.OuterProduct(m);
            var ev = LinearAlgebra.RealSymmetricEigenvalues(S, S.Trace * this.MeanPlaneTolerance);
            if (ev.z * this.MeanPlaneRatioSquared >= ev.y)
            {
                return null;
            }
            var EV = LinearAlgebra.RealSymmetricEigenvector(S, ev.z, S.Trace * this.MeanPlaneTolerance);
            return EV != null ? new Plane3(EV, EV * m) : null;
        }
    }
}
