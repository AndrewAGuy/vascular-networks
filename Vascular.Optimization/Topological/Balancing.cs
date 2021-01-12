using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Lattices;
using Vascular.Geometry.Lattices.Manipulation;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Topological
{
    public static class Balancing
    {
        public static IEnumerable<BranchAction> Ratio(Branch branch, double flowRatio, double rqRatio,
            ClosestBasisFunction toIntegral, Vector3[] connections)
        {
            if (branch.End is not Bifurcation bifurcation)
            {
                return Array.Empty<BranchAction>();
            }

            var p = bifurcation.Upstream;
            var c0 = bifurcation.Downstream[0];
            var c1 = bifurcation.Downstream[1];
            var Q0 = c0.Flow;
            var Q1 = c1.Flow;
            var RQ0 = Q0 * c0.ReducedResistance;
            var RQ1 = Q1 * c1.ReducedResistance;
            var (cH, QH, RQH, cL, QL, RQL) = Q0 < Q1
                ? (c1, Q1, RQ1, c0, Q0, RQ0)
                : (c0, Q0, RQ0, c1, Q1, RQ1);
            if (QH > QL * flowRatio)
            {
                if (RQH > RQL * rqRatio)
                {
                    // Flow imbalanced, matched by larger radius on that side.
                    // Attempt to fix by moving smaller side downstream
                    return cH.Children.ArgMin(b => b.Flow, out var target, out var Q)
                        ? (new BranchAction[] { new MoveBifurcation(cL, target) })
                        : Array.Empty<BranchAction>();
                }
                else if (RQL > RQH * rqRatio)
                {
                    // Flow imbalanced, but radius imbalanced in the other direction?
                    // How does this even happen - cull lower flow, larger radius side.
                    return new BranchAction[] { new RemoveBranch(cL) };
                }
                else
                {
                    // Flow imbalanced but radius balanced. Try to fix by moving terminals
                    // from the high flow side to the low flow side? This may slightly
                    // aggravate radius imbalance.
                    return TransferTerminals(cH, cL, toIntegral, connections);
                }
            }
            else
            {
                // Reassign based on radius now - this means we only make one comparison next
                (cH, RQH, cL, RQL) = RQ0 < RQ1
                    ? (c1, RQ1, c0, RQ0)
                    : (c0, RQ0, c1, RQ1);
                if (RQH > RQL * rqRatio)
                {
                    // Flow balanced, but one side larger than other. Suggests excess length on
                    // that side, or a shortage on the other.
                    return ExchangeTerminals(cL, cH, toIntegral, connections);
                }
                else
                {
                    return Array.Empty<BranchAction>();
                }
            }
        }

        public static IEnumerable<BranchAction> ExchangeTerminals(Branch a, Branch b,
            ClosestBasisFunction toIntegral, Vector3[] connections)
        {
            return TerminalActionBase(a, b, toIntegral, connections, (x, y) => new SwapEnds(x, y));
        }

        public static IEnumerable<BranchAction> TransferTerminals(Branch from, Branch to,
            ClosestBasisFunction toIntegral, Vector3[] connections)
        {
            return TerminalActionBase(from, to, toIntegral, connections, (f, t) => new MoveBifurcation(f, t));
        }

        private static IEnumerable<BranchAction> TerminalActionBase(Branch a, Branch b,
            ClosestBasisFunction toIntegral, Vector3[] connections, Func<Branch, Branch, BranchAction> create)
        {
            var IA = LatticeActions.GetMultipleInterior<List<Terminal>>(a, toIntegral);
            var IB = LatticeActions.GetMultipleInterior<List<Terminal>>(b, toIntegral);
            foreach (var ia in IA)
            {
                var za = ia.Key;
                var Ta = ia.Value;
                foreach (var c in connections)
                {
                    var zb = za + c;
                    if (IB.TryGetValue(zb, out var Tb))
                    {
                        foreach (var ta in Ta)
                        {
                            foreach (var tb in Tb)
                            {
                                yield return create(ta.Upstream, tb.Upstream);
                            }
                        }
                    }
                }
            }
        }
    }
}
