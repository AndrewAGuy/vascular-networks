using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// 
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
        /// 
        /// </summary>
        public Func<TriangleIntersection, bool> PermittedIntersection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Branch, bool> CandidateConnection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double TerminalSearchLength { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double TerminalEndFraction { get; set; } = 1e-3;

        /// <summary>
        /// 
        /// </summary>
        public Func<Network, double> TerminalFlowRate { get; set; } = n => 1;

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
        /// 
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

            MakeBifurcations(A, T, S, X);
            return T;
        }

        private void MakeBifurcations(Attachment[] A, Terminal[] T, Segment[] S, Vector3[] X)
        {
            for (var i = 0; i < A.Length; ++i)
            {
                if (T[i] is not null)
                {
                    continue;
                }

                var n = S[i].Network();
                T[i] = new Terminal(X[i], this.TerminalFlowRate(n))
                {
                    Network = n
                };
                var b = Topology.CreateBifurcation(S[i], T[i]);
                b.Position = X[i];
            }
        }

        private static bool CheckFeasibility(Terminal[] T, Segment[] S)
        {
            for (var i = 0; i < T.Length; ++i)
            {
                if (T[i] is null && S[i] is null)
                {
                    return false;
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
            var C = new List<Branch>[N.Length];
            for (var i = 0; i < N.Length; ++i)
            {
                C[i] = new List<Branch>();
                var AB = new AxialBounds(A[i].Position, this.TerminalSearchLength);
                N[i].Query(AB, (Branch b) =>
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
                var I = B.Evaluate(n);
                if (!I.All(this.PermittedIntersection))
                {
                    return false;
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
