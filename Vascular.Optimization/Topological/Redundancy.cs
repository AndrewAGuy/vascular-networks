using System;
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
        /// <param name="weighting"></param>
        /// <param name="degree"></param>
        /// <returns></returns>
        public static double EstimateChange(BranchAction action, Func<Branch, double> weighting, double degree)
        {
            // TODO - implement something along the lines of degree ^ -depth(greatest ancestor)
            // Somehow incoroprate weighting, maybe also consider distance between nodes as well.
            switch (action)
            {
                case MoveBifurcation:
                    throw new NotImplementedException();

                case SwapEnds:
                    throw new NotImplementedException();
            }
            return double.PositiveInfinity;
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
