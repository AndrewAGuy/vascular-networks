using System;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC.Defaults
{
    /// <summary>
    /// Delegates related to the spreading phase.
    /// </summary>
    public static class Spread
    {
        /// <summary>
        /// Simple heuristic for bifurcation placement.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3 FlowWeightedPosition(Bifurcation b)
        {
            var c0 = b.Downstream[0].End;
            var c1 = b.Downstream[1].End;
            var w0 = c0.Flow;
            var w1 = c1.Flow;
            var wP = w0 + w1;
            var p =
                w0 * c0.Position +
                w1 * c1.Position +
                wP * b.Upstream.Start.Position;
            return p / (2.0 * wP);
        }

        /// <summary>
        /// Simple heuristic for bifurcation placement, only for local segments rather than branches.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3 LocalFlowWeightedPosition(Bifurcation b)
        {
            var c0 = b.Children[0].End;
            var c1 = b.Children[1].End;
            var w0 = c0.Flow();
            var w1 = c1.Flow();
            var wP = w0 + w1;
            var p =
                w0 * c0.Position +
                w1 * c1.Position +
                wP * b.Parent.Start.Position;
            return p / (2.0 * wP);
        }

        /// <summary>
        /// Simple heuristic for bifurcation placement.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3 MeanPosition(Bifurcation b)
        {
            return (b.Downstream[0].End.Position + b.Downstream[1].End.Position + b.Upstream.Start.Position) / 3.0;
        }

        /// <summary>
        /// Rank by ratios of lengths in the bifurcation triad.
        /// </summary>
        /// <param name="maxPower"></param>
        /// <param name="minPower"></param>
        /// <returns></returns>
        public static TerminalPairCostFunction TerminalPairLengthRatioCost(double maxPower = 2, double minPower = 1)
        {
            return (T, t) =>
            {
                var pt = t.Position;
                var pT = T.Position;
                var pU = T.Upstream.Start.Position;
                var ltT = Vector3.Distance(pt, pT);
                var ltU = Vector3.Distance(pt, pU);
                var lTU = Vector3.Distance(pT, pU);
                var max = Math.Max(Math.Max(ltT, ltU), lTU);
                var min = Math.Min(Math.Min(ltT, ltU), lTU);
                return Math.Pow(max, maxPower) / Math.Pow(min, minPower);
            };
        }

        /// <summary>
        /// Disallow sharp and shallow angles.
        /// </summary>
        /// <param name="dotMax"></param>
        /// <param name="dotMin"></param>
        /// <returns></returns>
        public static TerminalPairPredicate TerminalPairAnglePredicate(double dotMax = 0.95, double dotMin = -0.25)
        {
            return (T, t) =>
            {
                var dt = (t.Position - T.Upstream.Start.Position).Normalize();
                var dT = T.Upstream.NormalizedDirection;
                var a = dt * dT;
                return a <= dotMax && a >= dotMin;
            };
        }

        /// <summary>
        /// Disallow bifurcations from narrow vessels.
        /// </summary>
        /// <param name="criticalRadius"></param>
        /// <returns></returns>
        public static TerminalPairPredicate TerminalPairRadiusPredicate(double criticalRadius)
        {
            return (T, t) => T.Upstream.Radius >= criticalRadius;
        }

        /// <summary>
        /// A simple cost, ranking by the distance to the direct line of the existing terminal branch.
        /// </summary>
        /// <returns></returns>
        public static TerminalPairCostFunction TerminalPairDistanceCost()
        {
            return (T, t) => LinearAlgebra.DistanceToLine(T.Upstream.Start.Position, T.Position, t.Position);
        }

        /// <summary>
        /// During <see cref="LatticeState.Spread"/>, newly added terminals do not trigger a propagation.
        /// This must be performed manually in <see cref="LatticeState.AfterSpreadAction"/> if optimization is to be performed.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="setRadii"></param>
        /// <returns></returns>
        public static Action UpdateLogicalAndPhysical(Network network, bool setRadii = true)
        {
            return () =>
            {
                network.Root.SetLogical();
                network.Source.CalculatePhysical();
                if (setRadii)
                {
                    network.Source.PropagateRadiiDownstream();
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Segment NearestSegmentSelector(Branch b, Terminal t)
        {
            return b.Segments.ArgMin(s => LinearAlgebra.DistanceToLine(s.Start.Position, s.End.Position, t.Position));
        }

        /// <summary>
        /// Uses the fact that child 0 of bifurcation is the previous terminal, recreates the old segment line from this.
        /// Ensures that the bifurcation lies at least <paramref name="minFraction"/> away from the endpoints of the line.
        /// </summary>
        /// <param name="minFraction"></param>
        /// <returns></returns>
        public static BifurcationPositionFunction NearestPointPosition(double minFraction = 0.125)
        {
            var m = minFraction;
            var M = 1 - minFraction;
            return b =>
            {
                var s = b.Parent.Start.Position;
                var e = b.Children[0].End.Position;
                var t = b.Children[1].End.Position;
                var d = e - s;
                var f = LinearAlgebra.LineFactor(s, d, t);
                return s + d * f.Clamp(m, M);
            };
        }
    }
}
