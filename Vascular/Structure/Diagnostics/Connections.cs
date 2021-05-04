using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Structure.Diagnostics
{
    public static class Connections
    {
        public static IEnumerable<Branch> Inconsistent(IEnumerable<Branch> branches)
        {
            return branches.Where(b => !CheckConsistency(b));
        }

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
