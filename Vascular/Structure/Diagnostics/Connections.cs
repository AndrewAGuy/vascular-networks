using System.Collections.Generic;
using System.Linq;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    /// Diagnose issues with <see cref="Branch"/> and <see cref="BranchNode"/> links.
    /// </summary>
    public static class Connections
    {
        /// <summary>
        /// Find all branches which are inconsistent.
        /// </summary>
        /// <param name="branches"></param>
        /// <returns></returns>
        public static IEnumerable<Branch> Inconsistent(IEnumerable<Branch> branches)
        {
            return branches.Where(b => !CheckConsistency(b));
        }

        /// <summary>
        /// Returns true if the branch can be recovered through the <see cref="BranchNode.Upstream"/> 
        /// of <see cref="Branch.End"/>, and for each child there is agreement between <see cref="Branch.End"/>
        /// of the upstream and <see cref="Branch.Start"/> of the downstream.
        /// Returns false if inconsistency detected.
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public static bool CheckConsistency(Branch branch)
        {
            if (!branch.IsTopologicallyValid)
            {
                return false;
            }
            foreach (var child in branch.Children)
            {
                if (child.Start != branch.End)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
