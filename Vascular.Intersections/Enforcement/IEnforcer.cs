using System.Threading.Tasks;

namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEnforcer
    {
        /// <summary>
        /// 
        /// </summary>
        bool CullingPermitted { get; set; }

        /// <summary>
        /// If a terminal is culled, cull its matched partners as well.
        /// </summary>
        bool CullMatched { get; set; }

        /// <summary>
        /// 
        /// </summary>
        bool ChangeTopology { get; set; }

        /// <summary>
        /// 
        /// </summary>
        bool ChangeGeometry { get; set; }

        /// <summary>
        /// Propagate flows. If false, calculates as a single pass at the end. 
        /// Can be more efficient when few changes are made.
        /// </summary>
        bool PropagateTopology { get; set; }

        /// <summary>
        /// Propagate reduced resistances and effective lengths. If false, calculates as a single pass at the end. 
        /// Can be more efficient when few changes are made.
        /// </summary>
        bool PropagateGeometry { get; set; }
        
        /// <summary>
        /// If no more violations remain, clear the <see cref="Penalizer{T}"/>. 
        /// Prevents bias against tricky intersections if more work is done before the next resolution step.
        /// </summary>
        bool ClearOnSuccess { get; set; }

        /// <summary>
        /// If the network is entirely deconstructed, throw a <see cref="TopologyException"/>.
        /// </summary>
        bool ThrowIfSourceCulled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="steps"></param>
        /// <returns></returns>
        Task<int> Advance(int steps);

        /// <summary>
        /// Calls <see cref="Advance(int)"/> until no more remain.
        /// </summary>
        /// <returns></returns>
        Task Resolve();

        /// <summary>
        /// The number of steps taken since <see cref="Advance(int)"/> was called.
        /// </summary>
        int Iterations { get; }
    }
}
