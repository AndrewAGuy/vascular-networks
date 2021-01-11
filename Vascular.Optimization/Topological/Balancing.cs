using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Topological
{
    public static class Balancing
    {
        public static bool TryGetAction(Branch branch, double flowRatio, double rqRatio)
        {
            if (branch.End is not Bifurcation bifurcation)
            {
                return false;
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
                    // Move downstream
                }
                else if (RQL > RQH * rqRatio)
                {
                    // Cull
                }
                else
                {
                    // Transfer terminals
                }
            }
        }
    }
}
