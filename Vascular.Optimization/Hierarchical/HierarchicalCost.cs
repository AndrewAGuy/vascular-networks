using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;

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
        ///
        /// </summary>
        /// <param name="network"></param>
        public abstract void SetCache(Network? network = null);

        /// <summary>
        ///
        /// </summary>
        public abstract double Cost { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public virtual (double cost, IDictionary<IMobileNode, Vector3> gradient) Evaluate(Network network)
        {
            var G = new Dictionary<IMobileNode, Vector3>(network.Nodes.Count());
            SetCache(network);
            foreach (var m in network.MobileNodes)
            {
                G[m] = PositionGradient(m);
            }
            return (this.Cost, G);
        }

        ///// <summary>
        /////
        ///// </summary>
        //public abstract HierarchicalGradients Cache { get; }
    }
}
