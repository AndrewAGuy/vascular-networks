using System;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Optimization.Topological
{
    /// <summary>
    /// Adds a competing cost to compare topology modifications by, potentially allowing actions
    /// that increase work, volume etc. to be made if it creates a redundant pathway between nodes,
    /// which may improve resilience to manufacturing errors such as failed branches.
    /// </summary>
    public static class Redundancy
    {
        /// <summary>
        /// Estimates change using <see cref="EstimateChange(MoveBifurcation, double, double, double)"/>
        /// and <see cref="EstimateChange(SwapEnds, double, double, double)"/>. Uses a measure of redundancy
        /// as the ratio of distance between two nodes compared to the expected crown length scale
        /// of their greatest common ancestor.
        /// <para/>
        /// Implemented as pairwise terms of 
        /// <c>abs(offset + sFactor * log(flow(GCA)) - d2Factor * log(dist2(a, b)))</c>.
        /// Appropriate scaling factors and offset will yield zero redundancy for a pair of terminals 
        /// joined by a bifurcation.
        /// Higher values indicate more redundancy, so for cost purposes this should be negated and
        /// scaled to an appropriate level to have an effect as a topological estimator.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="sFactor"></param>
        /// <param name="d2Factor"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static double EstimateChange(BranchAction action,
            double sFactor = 1.0 / 3.0, double d2Factor = 0.5, double offset = 0.0)
        {
            // Use ratio of expected separation to actual separation as a proxy for redundancy
            // So high expected, low actual and vice versa indicate that redundant pathways are present
            // Make symmetric by taking abs(log(ratio))
            return action switch
            {
                MoveBifurcation move => EstimateChange(move, sFactor, d2Factor),
                SwapEnds swap => EstimateChange(swap, sFactor, d2Factor),
                _ => double.NegativeInfinity
            };
        }

        /// <summary>
        /// See <see cref="EstimateChange(BranchAction, double, double, double)"/>.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="sFactor"></param>
        /// <param name="d2Factor"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static double EstimateChange(MoveBifurcation move,
            double sFactor = 1.0 / 3.0, double d2Factor = 0.5, double offset = 0.0)
        {
            // Do not need to consider (target, sibling) pair, as same in both.
            var moving = move.A.End;
            var target = move.B.End;
            var sibling = move.A.FirstSibling.End;
            var gca = Branch.CommonAncestor(moving.Upstream, target.Upstream);
            var d2MT = d2Factor * Math.Log(Vector3.DistanceSquared(moving.Position, target.Position));
            var d2MS = d2Factor * Math.Log(Vector3.DistanceSquared(moving.Position, sibling.Position));
            var sMS = sFactor * Math.Log(moving.Flow + sibling.Flow);
            var sGCA = sFactor * Math.Log(gca.Flow);
            var sMT = sFactor * Math.Log(moving.Flow + target.Flow);
            var dR = Math.Abs(offset + d2MT - sMT) + Math.Abs(offset + d2MS - sGCA)
                - Math.Abs(offset + d2MS - sMS) - Math.Abs(offset + d2MT - sGCA);
            return dR;
        }

        /// <summary>
        /// See <see cref="EstimateChange(BranchAction, double, double, double)"/>.
        /// </summary>
        /// <param name="swap"></param>
        /// <param name="sFactor"></param>
        /// <param name="d2Factor"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static double EstimateChange(SwapEnds swap,
            double sFactor = 1.0 / 3.0, double d2Factor = 0.5, double offset = 0.0)
        {
            // Do not need to consider (a, d) and (b, c).
            var a = swap.A.FirstSibling.End;
            var b = swap.A.End;
            var c = swap.B.End;
            var d = swap.B.FirstSibling.End;
            var gca = Branch.CommonAncestor(b.Upstream, c.Upstream);
            var d2AB = d2Factor * Math.Log(Vector3.DistanceSquared(a.Position, b.Position));
            var d2AC = d2Factor * Math.Log(Vector3.DistanceSquared(a.Position, c.Position));
            var d2BD = d2Factor * Math.Log(Vector3.DistanceSquared(b.Position, d.Position));
            var d2CD = d2Factor * Math.Log(Vector3.DistanceSquared(c.Position, d.Position));
            var sAB = sFactor * Math.Log(a.Flow + b.Flow);
            var sCD = sFactor * Math.Log(c.Flow + d.Flow);
            var sGCA = sFactor * Math.Log(gca.Flow);
            var sAC = sFactor * Math.Log(a.Flow + c.Flow);
            var sBD = sFactor * Math.Log(b.Flow + d.Flow);
            var gain = Math.Abs(offset + d2AB - sGCA) + Math.Abs(offset + d2AC - sAC) 
                + Math.Abs(offset + d2BD - sBD) + Math.Abs(offset + d2CD - sGCA);
            var loss = Math.Abs(offset + d2AB - sAB) + Math.Abs(offset + d2AC - sGCA) 
                + Math.Abs(offset + d2BD - sGCA) + Math.Abs(offset + d2CD - sCD);
            return gain - loss;
        }

        /// <summary>
        /// As usual, estimate lengths proportional to Q^1/3 and radius as a fraction of that.
        /// </summary>
        /// <param name="L0"></param>
        /// <param name="fraction"></param>
        /// <returns></returns>
        public static Func<Branch, double> FlowRadius(double L0, double fraction = 1)
        {
            var r0 = L0 * fraction;
            return b => Math.Pow(b.Flow, 1.0 / 3.0) * r0;
        }

        /// <summary>
        /// For use with internal collider redundancy detection. Prevents rewiring near-terminal vessels
        /// with near-root vessels in the case where this is not prevented by the <see cref="TopologyAction.IsPermissible"/> check.
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="passAllBelow">If the larger flow rate is below this, actions are permitted regardless of ratio</param>
        /// <returns></returns>
        public static Func<Branch, Branch, bool> FlowSimilarPredicate(double ratio, double passAllBelow = 0)
        {
            return (a, b) =>
            {
                var (QL, QH) = a.Flow < b.Flow
                    ? (a.Flow, b.Flow)
                    : (b.Flow, a.Flow);
                return QH < passAllBelow
                    || QL * ratio >= QH;
            };
        }
    }
}
