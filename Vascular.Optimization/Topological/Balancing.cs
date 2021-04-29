using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Lattices.Manipulation;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Diagnostics;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Topological
{
    /// <summary>
    /// Topological actions based on bifurcation balance.
    /// </summary>
    public static class Balancing
    {
        /// <summary>
        /// Identifies branches that are too long for their flow rate, where length should scale as volume (hence flow) ^1/3.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="L0">The target length of a branch that carries 1 unit of flow.</param>
        /// <param name="lengthRatio">The factor of <paramref name="L0"/> before removing.</param>
        /// <returns>True if the branch is too long, false otherwise.</returns>
        public static bool LengthFlowRatio(Branch branch, double L0, double lengthRatio)
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
            return L > LT * lengthRatio;
        }

        /// <summary>
        /// Small terminal vessels are an issue as it can lead to tiny (or even zero) values of <see cref="Branch.ReducedResistance"/> in their
        /// upstream branches. This has lead to floating point issues in <see cref="Bifurcation.ReducedResistance"/> through a 0/0 error,
        /// and might also have the potential for overflow. 
        /// <para/>
        /// While one option is to have <see cref="Terminal.ReducedResistance"/> return a small non-zero value, an alternative viewpoint is that
        /// these terminals should not exist at all as something else has decided that their parent vessels should consume the terminal site.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="Lmin"></param>
        /// <param name="onCull"></param>
        public static void RemoveShortTerminals(Network network, double Lmin, Action<Terminal> onCull = null)
        {
            onCull ??= t => { };
            var removing = network.Terminals.Where(t => t.Upstream.Length <= Lmin).ToList();
            foreach (var terminal in removing)
            {
                onCull(terminal);
                // Always choose to propagate as this should only happen very rarely.
                Topology.CullTerminalAndPropagate(terminal, true);
            }
        }

        /// <summary>
        /// Similar to <see cref="RemoveShortTerminals(Network, double, Action{Terminal})"/> but can be more
        /// efficient as no allocations are made if the stack is appropriately sized.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="enumerator"></param>
        /// <param name="Lmin"></param>
        /// <param name="onCull"></param>
        public static void RemoveShortTerminals(Branch branch, BranchEnumerator enumerator, double Lmin, Action<Terminal> onCull = null)
        {
            onCull ??= t => { };
            var removing = enumerator.Terminals(branch)
                .Where(t => t.Upstream.Length <= Lmin).ToList();
            foreach (var terminal in removing)
            {
                onCull(terminal);
                Topology.CullTerminalAndPropagate(terminal, true);
            }
        }

        /// <summary>
        /// Try to move bifurcations if they are highly unbalanced in either flow or radius.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="flowRatio"></param>
        /// <param name="rqRatio"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Tries to swap the crowns of the branches.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="toIntegral"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public static IEnumerable<BranchAction> ExchangeTerminals(Branch a, Branch b,
            ClosestBasisFunction toIntegral, Vector3[] connections)
        {
            return TerminalActionBase(a, b, toIntegral, connections, (x, y) => new SwapEnds(x, y));
        }

        /// <summary>
        /// Tries to offload terminals to another crown.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="toIntegral"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Given a <paramref name="root"/> vessel, create the interior using <paramref name="toIntegral"/>.
        /// Then for every terminal downstream of <paramref name="from"/>, find all connected terminals
        /// (possibly also searching the local area, if <paramref name="tryAddLocal"/> is specified).
        /// For each candidate terminal which is not preceded by <paramref name="from"/>, creates an action
        /// that moves the offloaded terminal to bifurcate from the candidate.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="from"></param>
        /// <param name="toIntegral"></param>
        /// <param name="connections"></param>
        /// <param name="enumerator"></param>
        /// <param name="tryAddLocal"></param>
        /// <returns></returns>
        public static IEnumerable<BranchAction> OffloadTerminals(Branch root, Branch from,
            ClosestBasisFunction toIntegral, Vector3[] connections, BranchEnumerator enumerator,
            bool tryAddLocal = false)
        {
            var interior = LatticeActions.GetMultipleInterior<List<Terminal>>(root, toIntegral);
            foreach (var terminal in enumerator.Terminals(root))
            {
                var index = toIntegral(terminal.Position);
                var candidates = LatticeActions.GetConnected<List<Terminal>>(interior, connections, index);

                if (tryAddLocal && interior.TryGetValue(index, out var local))
                {
                    candidates.AddRange(local);
                }

                foreach (var candidate in candidates)
                {
                    if (!from.IsAncestorOf(candidate.Upstream))
                    {
                        yield return new MoveBifurcation(terminal.Upstream, candidate.Upstream);
                    }
                }
            }
        }
    }
}
