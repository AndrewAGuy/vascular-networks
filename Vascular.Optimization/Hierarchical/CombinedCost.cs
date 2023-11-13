using System;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization.Hierarchical
{
    /// <summary>
    /// Combines multiple <see cref="HierarchicalCost"/> instances into single cost.
    /// </summary>
    public class CombinedCost : HierarchicalCost
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="costs"></param>
        /// <param name="combiner"></param>
        /// <param name="gradient"></param>
        public CombinedCost(HierarchicalGradients cache, HierarchicalCost[] costs,
            Func<HierarchicalCost[], double> combiner, Func<HierarchicalCost[], double[]> gradient)
        {
            this.cache = cache;
            this.costs = costs;
            this.combiner = combiner;
            this.gradient = gradient;
        }

        private readonly HierarchicalCost[] costs;
        private readonly Func<HierarchicalCost[], double> combiner;
        private readonly Func<HierarchicalCost[], double[]> gradient;

        private double[] gradients = null!;
        private readonly HierarchicalGradients cache;

        /// <summary>
        /// Defers to combination function.
        /// </summary>
        public override double Cost => combiner(costs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override Vector3 PositionGradient(IMobileNode node)
        {
            var gradient = new Vector3();
            for (var i = 0; i < gradients.Length; ++i)
            {
                gradient += costs[i].PositionGradient(node) * gradients[i];
            }
            return gradient;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public override double FlowGradient(Branch branch)
        {
            var gradient = 0.0;
            for (var i = 0; i < gradients.Length; ++i)
            {
                gradient += costs[i].FlowGradient(branch) * gradients[i];
            }
            return gradient;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public override double ReducedResistanceGradient(Branch branch)
        {
            var gradient = 0.0;
            for (var i = 0; i < gradients.Length; ++i)
            {
                gradient += costs[i].ReducedResistanceGradient(branch) * gradients[i];
            }
            return gradient;
        }

        /// <summary>
        /// If new network is specified, first sets the underlying base <see cref="HierarchicalGradients"/> cache to
        /// target this network. Then sets base cache and all costs depending on this, and updates gradients of
        /// overall cost with respect to each component cost.
        /// </summary>
        /// <param name="n"></param>
        public override void SetCache(Network? n)
        {
            if (n is not null)
            {
                cache.Network = n;
            }
            cache.SetCache();

            foreach (var cost in costs)
            {
                cost.SetCache(null);
            }
            gradients = gradient(costs);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public override double SetCost(Network? n)
        {
            if (n is not null)
            {
                cache.Network = n;
            }
            foreach (var cost in costs)
            {
                cost.SetCost(n);
            }
            return this.Cost;
        }
    }
}
