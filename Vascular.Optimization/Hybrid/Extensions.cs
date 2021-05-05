using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Optimization.Topological;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Hybrid
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Uses the approaches from <see cref="Grouping"/>. Groups are extracted when they are
        /// shorter than <paramref name="minRatio"/> times the expected length for a given flow
        /// rate, determined using <paramref name="unitLength"/> for the length of the unit flow
        /// branch. If <paramref name="onlyEndpoints"/> is false, all branches in between the
        /// grouping parent and endpoints are considered for actions. If <paramref name="multipleCandidates"/>
        /// is true, all generated actions will be returned, otherwise the minimum suitable
        /// action will be returned.
        /// </summary>
        /// <param name="hm"></param>
        /// <param name="unitLength"></param>
        /// <param name="minRatio"></param>
        /// <param name="onlyEndpoints"></param>
        /// <param name="multipleCandidates"></param>
        /// <returns></returns>
        public static HybridMinimizer AddRegroup(this HybridMinimizer hm,
            double unitLength, double minRatio, bool onlyEndpoints = false, bool multipleCandidates = true)
        {
            IEnumerable<BranchAction> generator()
            {
                foreach (var (parent, endpoints) in Grouping.LengthFlowRatioDownstream(
                        hm.Network.Root, unitLength, minRatio))
                {
                    var permissible = Grouping.PermissibleActions(endpoints, parent, onlyEndpoints);
                    if (multipleCandidates)
                    {
                        foreach (var action in permissible)
                        {
                            yield return action;
                        }
                    }
                    else
                    {
                        if (hm.ActionPredicate != null)
                        {
                            if (permissible.MinSuitable(
                                    hm.EstimateChange, hm.ActionPredicate,
                                    out var optimal, out var dC) &&
                                dC < hm.CostChangeThreshold)
                            {
                                yield return optimal;
                            }
                        }
                        else if (permissible.ArgMin(hm.EstimateChange, out var optimal, out var dC) &&
                            dC < hm.CostChangeThreshold)
                        {
                            yield return optimal;
                        }
                    }
                }
            }

            hm.AddTopologySource(generator);
            return hm;
        }

        /// <summary>
        /// Uses the approach from <see cref="Balancing"/>. Always uses <paramref name="flowRatio"/>
        /// and <paramref name="radiusRatio"/>, which is converted into a QR* ratio, using
        /// <see cref="Balancing.BifurcationRatio(Branch, double, double)"/>.
        /// Optionally uses <see cref="Balancing.LengthFlowRatio(Branch, double, double)"/> to
        /// determine if a branch is too long if <paramref name="allowRemoval"/> is true, using
        /// <paramref name="maxLengthRatio"/> and <paramref name="unitLength"/>.
        /// Removal behaviour can be modified: if <paramref name="offloadRemoved"/> is true,
        /// uses the <see cref="HybridMinimizer.Interior"/> representation to attempt to offload
        /// terminals using <see cref="Balancing.OffloadTerminals(Branch, Dictionary
        /// {Geometry.Vector3, ICollection{Terminal}}, Geometry.Lattices.Manipulation.ClosestBasisFunction, 
        /// Geometry.Vector3[], Func{Branch, IEnumerable{Terminal}}, bool)"/>, with 
        /// <paramref name="tryLocalTerminals"/>. If <paramref name="persistRemove"/> is true,
        /// additionally returns the remove action, such that an attempt is made to offload terminals
        /// but the branch is also removed afterwards. If using, ensure that remove actions are
        /// appropriately costed to act last.
        /// </summary>
        /// <param name="hm"></param>
        /// <param name="flowRatio"></param>
        /// <param name="radiusRatio"></param>
        /// <param name="unitLength"></param>
        /// <param name="maxLengthRatio"></param>
        /// <param name="allowRemoval"></param>
        /// <param name="offloadRemoved"></param>
        /// <param name="persistRemove"></param>
        /// <param name="tryLocalTerminals"></param>
        /// <returns></returns>
        public static HybridMinimizer AddRebalance(this HybridMinimizer hm,
            double flowRatio, double radiusRatio, double unitLength, double maxLengthRatio,
            bool allowRemoval, bool offloadRemoved, bool persistRemove, bool tryLocalTerminals)
        {
            IEnumerable<BranchAction> generator()
            {
                if (allowRemoval && offloadRemoved)
                {
                    hm.SetInterior();
                }
                var rqRatio = Math.Pow(radiusRatio, 4);
                foreach (var b in hm.Branches)
                {
                    var rebalance = Balancing.BifurcationRatio(b, flowRatio, rqRatio);
                    if (rebalance == null &&
                        Balancing.LengthFlowRatio(b, unitLength, maxLengthRatio))
                    {
                        rebalance = new RemoveBranch(b);
                    }

                    if (rebalance is RemoveBranch remove && allowRemoval)
                    {
                        remove.OnCull = hm.OnCull;
                        if (offloadRemoved)
                        {
                            var offload = Balancing.OffloadTerminals(
                                remove.A, hm.Interior, hm.ToIntegral, hm.Connections,
                                br => Terminal.GetDownstream(br, hm.TerminalCountEstimate(br)),
                                tryLocalTerminals);
                            foreach (var action in offload.Where(o => o.IsPermissible()))
                            {
                                yield return action;
                            }

                            if (persistRemove)
                            {
                                yield return remove;
                            }
                        }
                        else
                        {
                            yield return remove;
                        }
                    }
                    else if (rebalance != null)
                    {
                        yield return rebalance;
                    }
                }
            }

            hm.AddTopologySource(generator);
            return hm;
        }

        /// <summary>
        /// Uses <see cref="Balancing.TerminalActions(Dictionary
        /// {Geometry.Vector3, ICollection{Terminal}}, Geometry.Lattices.Manipulation.ClosestBasisFunction, 
        /// Geometry.Vector3[], bool, Func{Terminal, IEnumerable{Branch}})"/> to generate all possible
        /// terminals actions.
        /// </summary>
        /// <param name="hm"></param>
        /// <param name="tryLocalTerminals"></param>
        /// <param name="expansion"></param>
        /// <returns></returns>
        public static HybridMinimizer AddTerminalActions(this HybridMinimizer hm,
            bool tryLocalTerminals, Func<Terminal,IEnumerable<Branch>> expansion)
        {
            IEnumerable<BranchAction> generator()
            {
                hm.SetInterior();
                var terminalActions = Balancing.TerminalActions(
                    hm.Interior, hm.ToIntegral, hm.Connections,
                    tryLocalTerminals, expansion);
                foreach (var action in terminalActions)
                {
                    yield return action;
                }
            }

            hm.AddTopologySource(generator);
            return hm;
        }

        /// <summary>
        /// Attempts to generate promotions for all nodes.
        /// </summary>
        /// <param name="hm"></param>
        /// <returns></returns>
        public static HybridMinimizer AddPromotions(this HybridMinimizer hm)
        {
            IEnumerable<BranchAction> generator()
            {
                foreach (var p in Grouping.Promotions(hm.Branches))
                {
                    yield return p;
                }
            }

            hm.AddTopologySource(generator);
            return hm;
        }

        /// <summary>
        /// Sets up the minimizer, topology estimator and preparing function for a
        /// <see cref="HierarchicalCosts"/> instance. Typically all that is required
        /// for most purposes.
        /// </summary>
        /// <param name="hm"></param>
        /// <param name="costs"></param>
        public static HybridMinimizer AddHierarchicalCosts(this HybridMinimizer hm, HierarchicalCosts costs)
        {
            hm.Minimizer.Add(n => costs.Evaluate());
            hm.AddTopologyEstimator(t => Grouping.EstimateCostChange(t, costs, hm.EvaluationPlacement));
            hm.AddEstimatorPrepare(() => costs.SetCache());
            return hm;
        }
    }
}
