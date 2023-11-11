using System.Collections.Generic;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    /// Optimization and collision resolution have been known to cause NaNs without proper care.
    /// In particular, normalizing branches with small lengths or reduced resistances at bifurcations
    /// attached to very small terminal vessels can cause NaN directly, or through generating Infs that
    /// then are divided/subtracted by another Inf. By testing for non-finite rather than NaN, we can
    /// catch future NaNs one step earlier.
    /// </summary>
    public static class NonFinite
    {
        /// <summary>
        /// Moves down the tree and returns the first instance on each path where a non-finite number is found.
        /// Tests node positions, branch lengths / flow / reduced resistances.
        /// </summary>
        /// <param name="net"></param>
        /// <returns></returns>
        public static IEnumerable<object> First(Network net)
        {
            var stack = new Stack<BranchNode>();
            stack.Push(net.Source!);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (TestNode(current))
                {
                    yield return current;
                }
                else
                {
                    foreach (var branch in current.Downstream)
                    {
                        if (TestBranch(branch))
                        {
                            yield return branch;
                        }
                        else
                        {
                            stack.Push(branch.End);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Starts from the terminals and works up until a non-finite number is found.
        /// </summary>
        /// <param name="net"></param>
        /// <returns></returns>
        public static IEnumerable<object> Last(Network net)
        {
            var last = new HashSet<object>();
            foreach (var t in net.Terminals)
            {
                var u = t.Upstream!;
                if (TestNode(t))
                {
                    last.Add(t);
                }
                else if (TestBranch(u))
                {
                    last.Add(u);
                }
                else if (TestNode(u.Start))
                {
                    last.Add(u.Start);
                }
                else
                {
                    foreach (var U in u.UpstreamTo(null))
                    {
                        if (TestBranch(U))
                        {
                            last.Add(U);
                            break;
                        }
                        else if (TestNode(U.Start))
                        {
                            last.Add(U.Start);
                            break;
                        }
                    }
                }
            }
            return last;
        }

        /// <summary>
        /// Checks position.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool TestNode(INode node)
        {
            return !node.Position.IsFinite;
        }

        /// <summary>
        /// Checks reduced resistance, length and flow.
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public static bool TestBranch(Branch branch)
        {
            return !double.IsFinite(branch.ReducedResistance)
                || !double.IsFinite(branch.Length)
                || !double.IsFinite(branch.Flow);
        }

        /// <summary>
        /// Checks length and radius.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static bool TestSegment(Segment segment)
        {
            return !double.IsFinite(segment.Length)
                || !double.IsFinite(segment.Radius);
        }
    }
}
