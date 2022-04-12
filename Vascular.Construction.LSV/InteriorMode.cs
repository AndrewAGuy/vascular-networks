using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSV
{
    /// <summary>
    /// How a <see cref="LatticeState"/> should behave on initialization, refinement and coarsening.
    /// Combines with 
    /// </summary>
    public enum InteriorMode
    {
        /// <summary>
        /// Uses single interior until coarsened into, then switches to multiple.
        /// </summary>
        Default,

        /// <summary>
        /// Always reduces down to single interior.
        /// </summary>
        Single,

        /// <summary>
        /// Always uses multiple interior, may filter elements for performance.
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Reduces a terminal collection from a multiple interior to a single terminal.
    /// </summary>
    /// <param name="z"></param>
    /// <param name="x"></param>
    /// <param name="T"></param>
    /// <returns></returns>
    public delegate Terminal InteriorReductionFunction(Vector3 z, Vector3 x, ICollection<Terminal> T);

    /// <summary>
    /// Modifies the elements in terminal collection.
    /// </summary>
    /// <param name="z"></param>
    /// <param name="x"></param>
    /// <param name="T"></param>
    public delegate void InteriorFilter(Vector3 z, Vector3 x, ICollection<Terminal> T);
}
