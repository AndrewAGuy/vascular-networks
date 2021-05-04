using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;


namespace Vascular.Optimization.Topological
{
    using Surface = IAxialBoundsQueryable<TriangleSurfaceTest>;

    /// <summary>
    /// Methods related to resolving issues caused by poor topology, leading to groups of bifurcations forming.
    /// </summary>
    public static class Grouping
    {
        /// <summary>
        /// Gets the <see cref="BranchNode"/> instances ending branches downstream of this that are longer than
        /// the suggested short length for the given flow rate. See also <see cref="Balancing.LengthFlowRatio(Branch, double, double)"/>
        /// for the opposite issue.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="L0"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public static ICollection<BranchNode> LengthFlowRatio(Branch parent, double L0, double ratio)
        {
            var endpoints = new HashSet<BranchNode>();
            foreach (var child in parent.Children)
            {
                LengthFlowRatio(child, L0 * ratio, endpoints);
            }
            return endpoints;
        }

        private static void LengthFlowRatio(Branch current, double L0r, ICollection<BranchNode> endpoints)
        {
            var LT = L0r * Math.Pow(current.Flow, 1.0 / 3.0);
            if (current.Length > LT ||
                current.Children.Length == 0)
            {
                endpoints.Add(current.End);
            }
            else
            {
                foreach (var child in current.Children)
                {
                    LengthFlowRatio(child, L0r, endpoints);
                }
            }
        }

        /// <summary>
        /// Starting from the root, group bifurcations which are linked by short branches. These groupings may later be resolved,
        /// for example using <see cref="ClosestPairs(ICollection{BranchNode}, Branch, double)"/>.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="L0"></param>
        /// <param name="ratio"></param>
        /// <param name="includeBifurcations"></param>
        /// <returns></returns>
        public static IEnumerable<(Branch parent, ICollection<BranchNode> endpoints)>
            LengthFlowRatioDownstream(Branch root, double L0, double ratio, bool includeBifurcations = false)
        {
            var stack = new Stack<Branch>();
            stack.Push(root);
            while (stack.Count != 0)
            {
                var current = stack.Pop();
                var endpoints = LengthFlowRatio(current, L0, ratio);
                if (endpoints.Count > 2 ||
                    endpoints.Count == 2 && includeBifurcations)
                {
                    yield return (current, endpoints);
                }
                foreach (var node in endpoints)
                {
                    stack.Push(node.Upstream);
                }
            }
        }

        /// <summary>
        /// A simple heuristic for resolving grouping issues. Will fix particularly egregious cases, in a similar manner to
        /// Georg et al.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="parent"></param>
        /// <param name="parentStartWeight"></param>
        public static void ClosestPairs(ICollection<BranchNode> endpoints, Branch parent, double parentStartWeight = 0.0)
        {
            while (endpoints.Count > 1)
            {
                // Get the closest pair of nodes
                var (a, b) = endpoints.Pairs().ArgMin(p => Vector3.DistanceSquared(p.a.Position, p.b.Position));

                // Deciding where to put the new bifurcation is a heuristic job, go for flow weighting
                var psW = (a.Flow + b.Flow) * parentStartWeight;
                var psX = parent.Start.Position * psW;
                var bf = new Bifurcation()
                {
                    Position = (a.Position * a.Flow + b.Position * b.Flow + psX) / (a.Flow + b.Flow + psW),
                    Network = parent.Network
                };

                // Rewire by segments first. Make sure that endpoints see new segment as parent,
                // bifurcation sees them as children, and segment sees both start and end.
                bf.Children[0] = new Segment()
                {
                    Start = bf,
                    End = a
                };
                a.Parent = bf.Children[0];
                bf.Children[1] = new Segment()
                {
                    Start = bf,
                    End = b
                };
                b.Parent = bf.Children[1];

                // Now create new branches
                var A = new Branch(bf.Children[0])
                {
                    Start = bf,
                    End = a
                };
                var B = new Branch(bf.Children[1])
                {
                    Start = bf,
                    End = b
                };
                // Update flow rates so we don't NaN
                A.UpdateLogical();
                B.UpdateLogical();

                // Pull the branch view into bifurcation
                bf.UpdateDownstream();

                // Update the endpoints
                endpoints.Remove(a);
                endpoints.Remove(b);
                endpoints.Add(bf);
            }

            // Wire the last segment of the existing parent branch into the remaining endpoint
            var node = endpoints.First();
            var seg = parent.Segments[^1];
            seg.End = node;
            node.Parent = seg;
            parent.End = node;
            parent.Reset();
        }

        /// <summary>
        /// Gets all branches involved in a grouping between the <see cref="BranchNode.Upstream"/> 
        /// branches of <paramref name="endpoints"/> up to <paramref name="parent"/>.
        /// If <paramref name="includeChildren"/> is specified, adds the children of <paramref name="endpoints"/>
        /// to the returned set. This can prevent bugs due to potentially invalid branches, in which the creation
        /// of new branches during <see cref="MoveBifurcation"/> actions causes a <see cref="KeyNotFoundException"/>
        /// in <see cref="HierarchicalCosts"/>.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="parent"></param>
        /// <param name="includeChildren"></param>
        /// <returns></returns>
        public static HashSet<Branch> AllInGrouping(IEnumerable<BranchNode> endpoints, Branch parent, bool includeChildren = false)
        {
            var branches = endpoints
                .Select(e => e.Upstream)
                .ToHashSet();
            foreach (var e in endpoints)
            {
                foreach (var u in e.Upstream.UpstreamTo(parent))
                {
                    branches.Add(u);
                }
            }
            branches.Add(parent);
            if (includeChildren)
            {
                foreach (var e in endpoints)
                {
                    foreach (var c in e.Downstream)
                    {
                        branches.Add(c);
                    }
                }
            }
            return branches;
        }

        /// <summary>
        /// Tests whether the branches in <paramref name="grouping"/> have been visited already.
        /// If so, returns false, as the branches may not have references in a <see cref="HierarchicalCosts"/>
        /// gradient cache.
        /// If not visited yet, adds the elements of <paramref name="grouping"/> to <paramref name="visited"/>
        /// and returns true.
        /// </summary>
        /// <param name="visited"></param>
        /// <param name="grouping"></param>
        /// <returns></returns>
        public static bool TryVisit(HashSet<Branch> visited, HashSet<Branch> grouping)
        {
            if (visited.Overlaps(grouping))
            {
                return false;
            }
            visited.UnionWith(grouping);
            return true;
        }

        /// <summary>
        /// For a bifurcation group, gets the permissible actions.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="parent"></param>
        /// <param name="onlyEndpoints"></param>
        /// <returns></returns>
        public static IEnumerable<BranchAction> PermissibleActions(
            IEnumerable<BranchNode> endpoints, Branch parent, bool onlyEndpoints = true)
        {
            var branches = onlyEndpoints
                ? endpoints.Select(n => n.Upstream)
                : AllInGrouping(endpoints, parent);
            // For each pair, work out if we can move A -> B, B -> A or swap A <-> B
            // Making an action will invalidate the cost cache for this grouping
            // So for all valid actions with negative cost impact, pick the smallest           
            return branches.Pairs()
                .SelectMany(p => BranchPairActions(p.a, p.b))
                .Where(a => a.IsPermissible());
        }

        /// <summary>
        /// Creates the two <see cref="MoveBifurcation"/> actions and a <see cref="SwapEnds"/>, regardless
        /// of whether any of them are possible. Test using <see cref="TopologyAction.IsPermissible"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static IEnumerable<BranchAction> BranchPairActions(Branch a, Branch b)
        {
            yield return new MoveBifurcation(a, b);
            yield return new MoveBifurcation(b, a);
            yield return new SwapEnds(a, b);
        }

        /// <summary>
        /// Returns all <see cref="MoveBifurcation"/> and <see cref="SwapEnds"/> that can be made from
        /// elements of <paramref name="A"/> and <paramref name="B"/>.
        /// Actions may not be distinct or valid if there are mutual elements, so filter afterwards.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static IEnumerable<BranchAction> PairwiseActions(IEnumerable<Branch> A, IEnumerable<Branch> B)
        {
            foreach (var a in A)
            {
                foreach (var b in B)
                {
                    yield return new MoveBifurcation(a, b);
                    yield return new MoveBifurcation(b, a);
                    yield return new SwapEnds(a, b);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 FlowWeightedPlacement(BranchNode p, BranchNode c, BranchNode t)
        {
            var Qab = c.Flow + t.Flow;
            var Pt = p.Position * Qab + c.Position * c.Flow + t.Position * t.Flow;
            return Pt / (2 * Qab);
        }

        /// <summary>
        /// Places the bifurcation into target <paramref name="t"/> at the closest point on
        /// the meanline between <paramref name="p"/> and <paramref name="c"/>.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 MeanlinePlacement(BranchNode p, BranchNode c, BranchNode t)
        {
            var dir = c.Position - p.Position;
            var fAB = LinearAlgebra.LineFactor(p.Position, dir, t.Position);
            return p.Position + fAB * dir;
        }

        /// <summary>
        /// Estimates the cost change of executing action <paramref name="action"/> under the costs
        /// <paramref name="costs"/>. For <see cref="MoveBifurcation"/> actions, uses <paramref name="placement"/>
        /// to decide where to place the bifurcation. Note that typical weightings might not be valid as the topology
        /// hasn't been changed at this point, so for flow weightings use 
        /// <see cref="FlowWeightedPlacement(BranchNode, BranchNode, BranchNode)"/> (if no function supplied, this is
        /// the default).
        /// </summary>
        /// <param name="action"></param>
        /// <param name="costs"></param>
        /// <param name="placement">
        /// For <see cref="MoveBifurcation"/> actions, takes the existing parent node,
        /// child node and target node (in that order) and returns the bifurcation position.
        /// </param>
        /// <returns></returns>
        public static double EstimateCostChange(BranchAction action, HierarchicalCosts costs,
            Func<BranchNode, BranchNode, BranchNode, Vector3> placement = null)
        {
            switch (action)
            {
                case MoveBifurcation m:
                    placement ??= FlowWeightedPlacement;
                    var p = placement(action.B.Start, action.B.End, action.A.End);
                    switch (m.A.End)
                    {
                        case Terminal t:
                            return costs.EstimatedChange(m.B, t, p);
                        case Bifurcation b:
                            return costs.EstimatedChange(m.B, b, p);
                    }
                    break;
                case SwapEnds s:
                    return costs.EstimatedChange(s.A, s.B);
            }
            return double.PositiveInfinity;
        }

        /// <summary>
        /// Gets the optimal action from <paramref name="actions"/>. For a description of <paramref name="placement"/>
        /// function, see <see cref="EstimateCostChange(BranchAction, HierarchicalCosts, Func{BranchNode, BranchNode, BranchNode, Vector3})"/>.
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="costs"></param>
        /// <param name="placement"></param>
        /// <returns></returns>
        public static (BranchAction a, double dC) OptimalAction(IEnumerable<BranchAction> actions, HierarchicalCosts costs,
            Func<BranchNode, BranchNode, BranchNode, Vector3> placement = null)
        {
            return actions.ArgMin(a => EstimateCostChange(a, costs, placement), out var optimal, out var dC)
                ? (optimal, dC)
                : (null, double.PositiveInfinity);
        }

        /// <summary>
        /// Gets all promotions that could be made, filtered by <see cref="TopologyAction.IsPermissible"/>.
        /// </summary>
        /// <param name="branches"></param>
        /// <returns></returns>
        public static IEnumerable<BranchAction> Promotions(IEnumerable<Branch> branches)
        {
            return branches
                .Select(b => new PromoteNode(b.End))
                .Where(p => p.IsPermissible())
                .Select(p => p.Action);
        }

        /// <summary>
        /// Ensures that the <see cref="BranchAction"/> <paramref name="action"/> will not create either
        /// a pair of branches or a bifurcation triad that will penetrate <paramref name="surface"/>,
        /// subject to a tolerance of <paramref name="rayTolerance"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="surface"></param>
        /// <param name="rayTolerance"></param>
        /// <returns></returns>
        public static bool EnsureNotLeaving(BranchAction action, Surface surface, double rayTolerance)
        {
            var ok = true;
            switch (action)
            {
                case MoveBifurcation move:
                    var tst = new TriangleSurfaceTest(
                        action.A.End.Position, action.B.Start.Position, action.B.End.Position,
                        rayTolerance);
                    // TODO: extend Query method to have a continuation predicate overload? Might be nice to terminate early.
                    // TODO: move this to somewhere else, it's a bit of a weird spot to be keeping this.
                    surface.Query(tst.GetAxialBounds(), TST =>
                    {
                        if (tst.TestTriangleRays(TST, out var a, out var b))
                        {
                            ok = false;
                        }
                    });
                    break;

                case SwapEnds swap:
                    var sa = swap.A.Start.Position;
                    var sb = swap.B.Start.Position;
                    var ea = swap.A.End.Position;
                    var eb = swap.B.End.Position;
                    if (surface.RayIntersectionCounts(sa, eb - sa, rayTolerance).outwards != 0 ||
                        surface.RayIntersectionCounts(sb, ea - sb, rayTolerance).outwards != 0)
                    {
                        ok = false;
                    }
                    break;
            }
            return ok;
        }
    }
}
