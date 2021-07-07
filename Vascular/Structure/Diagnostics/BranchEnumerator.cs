using System.Collections.Generic;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    /// Utility class for enumerating downstream branches with a common stack that
    /// can be reset if desired.
    /// </summary>
    public class BranchEnumerator
    {
        private Stack<Branch> stack = new();

        /// <summary>
        /// Sets capacity to zero, since the stack should be empty.
        /// </summary>
        public void Reset()
        {
            stack.TrimExcess();
        }

        /// <summary>
        /// Creates a new stack with the desired capacity.
        /// </summary>
        /// <param name="count"></param>
        public void Reset(int count)
        {
            stack = new(count);
        }

        /// <summary>
        /// Same as <see cref="Branch.DownstreamOf"/>, but does not allocate/deallocate a new stack.
        /// If <paramref name="include"/> is true, returns <paramref name="branch"/> as well.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="include"></param>
        /// <returns></returns>
        public IEnumerable<Branch> Downstream(Branch branch, bool include = true)
        {
            stack.Clear();
            if (include)
            {
                yield return branch;
            }

            foreach (var c in branch.Children)
            {
                stack.Push(c);
            }
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;
                var children = current.Children;
                for (var i = 0; i < children.Length; ++i)
                {
                    stack.Push(children[i]);
                }
            }
        }

        /// <summary>
        /// Returns all mobile nodes in branches downstream of and including <paramref name="root"/>.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public IEnumerable<IMobileNode> MobileNodes(Branch root)
        {
            foreach (var branch in Downstream(root, true))
            {
                if (branch.Start is IMobileNode mobile)
                {
                    yield return mobile;
                }
                foreach (var transient in branch.Transients)
                {
                    yield return transient as IMobileNode;
                }
            }
        }

        /// <summary>
        /// Returns all terminals downstream of <paramref name="branch"/>, including the end node.
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public IEnumerable<Terminal> Terminals(Branch branch)
        {
            stack.Clear();
            stack.Push(branch);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.End is Terminal terminal)
                {
                    yield return terminal;
                }
                else
                {
                    var children = current.Children;
                    for (var i = 0; i < children.Length; ++i)
                    {
                        stack.Push(children[i]);
                    }
                }
            }
        }
    }
}
