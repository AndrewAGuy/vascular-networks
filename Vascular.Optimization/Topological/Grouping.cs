using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Topological
{
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
            var psW = parent.Flow * parentStartWeight;
            var psX = parent.Start.Position * psW;
            while (endpoints.Count > 1)
            {
                // Get the closest pair of nodes
                var (a, b) = endpoints.Pairs().ArgMin(p => Vector3.DistanceSquared(p.a.Position, p.b.Position));

                // Deciding where to put the new bifurcation is a heuristic job, go for flow weighting
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
    }
}
