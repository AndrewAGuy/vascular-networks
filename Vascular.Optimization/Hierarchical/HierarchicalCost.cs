using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public abstract void SetCache();

        /// <summary>
        /// 
        /// </summary>
        public abstract double Cost { get; }
    }
}
