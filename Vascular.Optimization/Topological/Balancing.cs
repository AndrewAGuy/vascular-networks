﻿using System;
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
        public static BranchAction LengthFlowRatio(Branch branch, double L0, double lengthRatio)
        {
            // L0 is the flow rate that a branch carrying 1 unit of flow should carry.
            // Length should scale as volume (hence flow) ^1/3.
            // Branches too short may have serious bifurcation asymmetry (e.g. very high flow, very short
            // because it's bifurcating into a terminal) which should be resolved by moving the terminal
            // downstream. This would be achieved in the bifurcation asymmetry remover.
            // If balanced but still short, then that is a geometric issue. Maybe prioritize work as the cost.
            // This is about dealing with branches that are too long.
            // Rather than try anything fancy, in this case we should simply try to offload terminals
            // from the crown or just outright remove the branch and rebuild into the gap
            var L = branch.Length;
            var Q = branch.Flow;
            var LT = L0 * Math.Pow(Q, 1.0 / 3.0);
            if (L > LT * lengthRatio)
            {
                return new RemoveBranch(branch);
            }
            return null;
        }

        public static BranchAction BifurcationRatio(Branch branch, double flowRatio, double rqRatio)
        {
            if (branch.End is not Bifurcation bifurcation)
            {
                return null;
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
                    if (cH.Children.ArgMin(b => b.Flow, out var target, out var Q))
                    {
                        return new MoveBifurcation(cL, target);
                    }
                }
                else if (RQL > RQH * rqRatio)
                {
                    // Flow imbalanced, but radius imbalanced in the other direction?
                    // How does this even happen - cull lower flow, larger radius side.
                    return new RemoveBranch(cL);
                }
            }
            else
            {
                (cH, RQH, cL, RQL) = RQ0 < RQ1
                    ? (c1, RQ1, c0, RQ0)
                    : (c0, RQ0, c1, RQ1);
                if (RQH > RQL * rqRatio)
                {
                    return new RemoveBranch(cL);
                }
            }
            return null;
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
