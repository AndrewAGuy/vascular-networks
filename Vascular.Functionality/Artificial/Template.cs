using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Geometry.Triangulation;
using Vascular.Intersections;
using Vascular.Intersections.Enforcement;
using Vascular.Intersections.Triangulation;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Functionality.Artificial
{
    using Surface = IIntersectionEvaluator<TriangleIntersection>;

    /// <summary>
    /// Takes a templated functional unit (e.g. designed in CAD) with a predetermined set of channels
    /// and a triangulated boundary, then places these at specified locations.
    /// </summary>
    public class Template : Discrete
    {
        /// <summary>
        /// 
        /// </summary>
        public Segment[] Channels { get; }

        /// <summary>
        /// 
        /// </summary>
        public Attachment[] Attachments { get; }

        /// <summary>
        /// 
        /// </summary>
        public Mesh Boundary { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="attachments"></param>
        /// <param name="boundary"></param>
        public Template(IEnumerable<Segment> channels, IEnumerable<Attachment> attachments, Mesh boundary)
        {
            this.Channels = channels.ToArray();
            this.Attachments = attachments.ToArray();
            this.Boundary = boundary;
        }

        /// <summary>
        /// 
        /// </summary>
        public Func<Vector3, Matrix3> Orientation { get; set; } = x => new();

        /// <summary>
        /// Return 0 to ignore, &gt; 0 to cull, &lt; 0 to reject.
        /// </summary>
        public Func<TriangleIntersection, int> PermittedIntersection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Action<Terminal> OnCull { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Branch, bool> CandidateConnection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double TerminalSearchLength { get; set; }

        /// <summary>
        /// Allows a small amount of intersection at the end points.
        /// </summary>
        public double TerminalEndFraction { get; set; } = 1e-3;

        /// <summary>
        /// 
        /// </summary>
        public Func<Network, double> TerminalFlowRate { get; set; } = n => 1;

        /// <summary>
        /// Gets the boundary of the forbidden region when placed at multiple locations.
        /// </summary>
        /// <param name="locations"></param>
        /// <returns></returns>
        public Mesh GetBoundary(IEnumerable<Vector3> locations)
        {
            var M = new Mesh();
            foreach (var x in locations)
            {
                var R = this.Orientation(x);
                var m = this.Boundary.Transform(R, x);
                M.Merge(m);
            }
            return M;
        }

        /// <summary>
        /// Gets the attachment points when placed at multiple locations.
        /// </summary>
        /// <param name="locations"></param>
        /// <returns></returns>
        public List<Attachment> GetAttachments(IEnumerable<Vector3> locations)
        {
            var A = new List<Attachment>();
            foreach (var x in locations)
            {
                var R = this.Orientation(x);
                A.AddRange(this.Attachments.Select(a =>
                {
                    var p = R * a.Position + x;
                    var d = R * a.Direction;
                    return new Attachment(p, d, a.Type);
                }));
            }
            return A;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public override IEnumerable<Segment> Insert(Vector3 position, Terminal[] connections)
        {
            var R = this.Orientation(position);
            Vector3 f(Vector3 x) => R * x + position;
            return this.Channels.Select(s => Segment.MakeDummy(f(s.Start.Position), f(s.End.Position), s.Radius));
        }

        /// <summary>
        /// If location is permitted, clears the site and attempts to connect nearby terminals.
        /// If no terminals available, tries to create bifurcations into the attachment points.
        /// </summary>
        /// <param name="networks"></param>
        /// <param name="locations"></param>
        /// <returns></returns>
        public override IEnumerable<(Vector3, Terminal[], Attachment[])>
            PrepareLocations(Network[] networks, IEnumerable<Vector3> locations)
        {
            foreach (var x in locations)
            {
                if (Prepare(networks, x, out var T, out var A))
                {
                    yield return (x, T, A);
                }
            }
        }

        private bool Prepare(Network[] N, Vector3 x, out Terminal[] T, out Attachment[] A)
        {
            T = null;
            Mesh M;
            (M, A) = BoundaryAt(x);

            var BT = new MeshTree(M);
            if (!LocationAvailable(BT, N))
            {
                return false;
            }

            T = MakeAttachments(BT.Tree, N, A);
            return T != null;
        }

        private Terminal[] MakeAttachments(IAxialBoundsQueryable<TriangleSurfaceTest> B, Network[] N, Attachment[] A)
        {
            var T = new Terminal[A.Length];
            var CB = Candidates(N, A);

            var S = new Segment[A.Length];
            var X = new Vector3[A.Length];
            for (var i = 0; i < A.Length; ++i)
            {
                T[i] = TryTerminal(A[i], CB[i], B);
                if (T[i] is null)
                {
                    (S[i], X[i]) = TryBifurcate(A[i], CB[i], B);
                }
            }

            if (!CheckFeasibility(T, S))
            {
                return null;
            }

            Connect(A, T, S, X);
            return T;
        }

        private void Connect(Attachment[] A, Terminal[] T, Segment[] S, Vector3[] X)
        {
            for (var i = 0; i < A.Length; ++i)
            {
                if (T[i] is not null)
                {
                    T[i].SetPosition(A[i].Position);
                }
                else
                {
                    var n = S[i].Network();
                    T[i] = new Terminal(A[i].Position, this.TerminalFlowRate(n))
                    {
                        Network = n
                    };
                    var b = Topology.CreateBifurcation(S[i], T[i]);
                    b.Position = X[i];
                }
            }
        }

        private static bool CheckFeasibility(Terminal[] T, Segment[] S)
        {
            for (var i = 0; i < T.Length; ++i)
            {
                if (T[i] is null)
                {
                    if (S[i] is null)
                    {
                        return false;
                    }
                }
                else
                {
                    for (var j = 0; j < i; ++j)
                    {
                        if (T[i] == T[j])
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private (Segment, Vector3) TryBifurcate(Attachment A, List<Branch> C, IAxialBoundsQueryable<TriangleSurfaceTest> S)
        {
            var d2Min = double.PositiveInfinity;
            Segment sMin = null;
            Vector3 xMin = null;

            foreach (var c in C)
            {
                foreach (var s in c.Segments)
                {
                    var x = LinearAlgebra.ClosestPoint(s.Start.Position, s.Direction, A.Position);
                    if (IsSuitable(S, A, x))
                    {
                        var d2 = Vector3.DistanceSquared(A.Position, x);
                        if (d2 < d2Min)
                        {
                            (d2Min, sMin, xMin) = (d2, s, x);
                        }
                    }
                }
            }

            return (sMin, xMin);
        }

        private Terminal TryTerminal(Attachment A, List<Branch> C, IAxialBoundsQueryable<TriangleSurfaceTest> S)
        {
            var T = new List<Terminal>(C.Count);
            foreach (var c in C)
            {
                if (c.End is not Terminal t)
                {
                    continue;
                }
                var F = S.IntersectionSequence(c).FirstOrDefault();
                if (F is not null)
                {
                    F.Segment.End = t;
                    t.Parent = F.Segment;
                    t.SetPosition(F.Segment.AtFraction(F.Fraction));
                    c.Reinitialize();
                }

                if (IsSuitable(S, A, c.Start.Position))
                {
                    T.Add(t);
                }
            }

            return T.Count != 0
                ? T.ArgMin(t => Vector3.DistanceSquared(t.Position, A.Position))
                : null;
        }

        private bool IsSuitable(IAxialBoundsQueryable<TriangleSurfaceTest> S, Attachment A, Vector3 x)
        {
            var d = A.Position - x;
            var I = S.RayIntersections(x, d, 0);
            if (I.Count != 0)
            {
                var m = I.Min(R => R.Fraction);
                if (m < 1 - this.TerminalEndFraction)
                {
                    return false;
                }
            }
            return true;
        }

        private List<Branch>[] Candidates(Network[] N, Attachment[] A)
        {
            var C = new List<Branch>[A.Length];
            for (var i = 0; i < A.Length; ++i)
            {
                C[i] = new List<Branch>();
                var AB = new AxialBounds(A[i].Position, this.TerminalSearchLength);
                N[A[i].Type].Query(AB, (Branch b) =>
                {
                    if (this.CandidateConnection(b))
                    {
                        C[i].Add(b);
                    }
                });
            }
            return C;
        }

        private bool LocationAvailable(Surface B, Network[] N)
        {
            foreach (var n in N)
            {
                var I = B.Evaluate(n).ToList();
                var P = I.Select(this.PermittedIntersection).ToArray();
                if (P.Any(p => p < 0))
                {
                    return false;
                }

                var C = new List<Terminal>();
                for (var i = 0; i < I.Count; ++i)
                {
                    if (P[i] > 0)
                    {
                        Terminal.ForDownstream(I[i].Segment.Branch, t => C.Add(t));
                    }
                }
                foreach (var c in C)
                {
                    this.OnCull?.Invoke(c);
                    Topology.CullTerminalAndPropagate(c);
                }
            }
            return true;
        }

        private (Mesh, Attachment[]) BoundaryAt(Vector3 x)
        {
            var R = this.Orientation(x);
            var M = this.Boundary.Transform(R, x);
            var A = new Attachment[this.Attachments.Length];
            for (var i = 0; i < A.Length; ++i)
            {
                var p = R * this.Attachments[i].Position + x;
                var d = R * this.Attachments[i].Direction;
                A[i] = new(p, d, this.Attachments[i].Type);
            }
            return (M, A);
        }
    }
}
