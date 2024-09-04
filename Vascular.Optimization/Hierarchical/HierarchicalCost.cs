using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Diagnostics;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Hierarchical
{
    /// <summary>
    /// Represents base type for costs such as Schreiner costs and work.
    /// </summary>
    public abstract class HierarchicalCost
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public abstract Vector3 PositionGradient(IMobileNode node);

        /// <summary>
        ///
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public abstract double FlowGradient(Branch branch);

        /// <summary>
        ///
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public abstract double ReducedResistanceGradient(Branch branch);

        /// <summary>
        /// Sets <see cref="Cost"/> and gradient caches required to evaluate
        /// <see cref="PositionGradient"/>, <see cref="FlowGradient"/>, <see cref="ReducedResistanceGradient"/>.
        /// </summary>
        /// <param name="source"></param>
        public abstract void SetCache(Source? source = null);

        /// <summary>
        /// Sets <see cref="Cost"/> and returns the value.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public abstract double SetCost(Source? source = null);

        /// <summary>
        ///
        /// </summary>
        public abstract double Cost { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public virtual (double cost, IDictionary<IMobileNode, Vector3> gradient) Evaluate(Source source)
        {
            if (source.Root is null)
            {
                return (0, new Dictionary<IMobileNode, Vector3>());
            }

            var s = 0;
            // TODO: make proper counting methods and enumeration on source, as well as network.
            // Or use enumerator more often?
            source.ForEach(b => s += b.Segments.Count);
            var G = new Dictionary<IMobileNode, Vector3>(s);

            SetCache(source);
            var e = new BranchEnumerator();
            foreach (var m in e.MobileNodes(source.Root))
            {
                G[m] = PositionGradient(m);
            }
            return (this.Cost, G);
        }
    }
}
