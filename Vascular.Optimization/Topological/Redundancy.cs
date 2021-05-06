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
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static double EstimateChange(BranchAction action,
            double sFactor = 1.0 / 3.0, double d2Factor = 0.5)
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

        public static double EstimateChange(MoveBifurcation move,
            double sFactor = 1.0 / 3.0, double d2Factor = 0.5)
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
            var dR = Math.Abs(d2MT - sMT) + Math.Abs(d2MS - sGCA)
                - Math.Abs(d2MS - sMS) - Math.Abs(d2MT - sGCA);
            return dR;
        }

        public static double EstimateChange(SwapEnds swap,
            double sFactor = 1.0 / 3.0, double d2Factor = 0.5)
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
            var sAC= sFactor* Math.Log(a.Flow + c.Flow);
            var sBD = sFactor * Math.Log(b.Flow + d.Flow);
            var gain = Math.Abs(d2AB - sGCA) + Math.Abs(d2AC - sAC) + Math.Abs(d2BD - sBD) + Math.Abs(d2CD - sGCA);
            var loss = Math.Abs(d2AB - sAB) + Math.Abs(d2AC - sGCA) + Math.Abs(d2BD - sGCA) + Math.Abs(d2CD - sCD);
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
