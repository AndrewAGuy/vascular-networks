using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;

namespace Vascular.Optimization.Topological
{
    /// <summary>
    /// Generates large numbers of <see cref="BranchAction"/> instances without any real guidance.
    /// </summary>
    public static class Global
    {
        /// <summary>
        /// Every possible action that could be made. Make sure to filter by <see cref="TopologyAction.IsPermissible"/>.
        /// </summary>
        /// <param name="branches"></param>
        /// <returns></returns>
        public static IEnumerable<BranchAction> All(IEnumerable<Branch> branches)
        {
            return Grouping.PairwiseActions(branches, branches);
        }

        /// <summary>
        /// Maps x -> <paramref name="ratio"/> * x, clamped to range <paramref name="min"/>, <paramref name="max"/>.
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Func<double, double> ClampedRatio(double ratio,
            double min = 0.0, double max = double.PositiveInfinity)
        {
            return Q => (Q * ratio).Clamp(min, max);
        }

        /// <summary>
        /// For all branches, generate actions between branches which have Qhi &lt;= Qlo * <paramref name="maxFlow"/>(Qlo)
        /// and dist(a.End, b.End) &lt;= <paramref name="maxLength"/>(Qlo).
        /// </summary>
        /// <param name="branches"></param>
        /// <param name="maxFlow"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static IEnumerable<BranchAction> FlowRatio(IEnumerable<Branch> branches,
            Func<double, double> maxFlow, Func<double, double>? maxLength = null)
        {
            maxLength ??= Q => double.PositiveInfinity;
            // Order by flow, then work upwards
            var ordered = branches.OrderBy(b => b.Flow).ToArray();
            for (var i = 0; i < ordered.Length; ++i)
            {
                var a = ordered[i];
                var Qmax = maxFlow(a.Flow);
                var Lmax2 = Math.Pow(maxLength(a.Flow), 2);
                for (var j = i + 1; j < ordered.Length; ++j)
                {
                    var b = ordered[j];
                    if (b.Flow > Qmax)
                    {
                        break;
                    }
                    else if (Vector3.DistanceSquared(a.End.Position, b.End.Position) > Lmax2)
                    {
                        continue;
                    }

                    foreach (var p in Grouping.BranchPairActions(a, b))
                    {
                        yield return p;
                    }
                }
            }
        }
    }
}
