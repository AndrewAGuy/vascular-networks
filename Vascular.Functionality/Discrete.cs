using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Functionality
{
    /// <summary>
    /// Represents a discrete functional unit that takes a specified vessel from each tree
    /// and merges them together through the functional structure.
    /// </summary>
    public abstract class Discrete
    {
        /// <summary>
        /// Filters the provided list of locations and ensures that space is free for those which are selected.
        /// </summary>
        /// <param name="networks"></param>
        /// <param name="locations"></param>
        /// <returns></returns>
        public abstract IEnumerable<(Vector3, Terminal[], Attachment[])> 
            PrepareLocations(Network[] networks, IEnumerable<Vector3> locations);

        /// <summary>
        /// Creates the functional structure and connects the specified terminals to it.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public abstract IEnumerable<Segment> Insert(Vector3 position, Terminal[] connections);
    }

    /// <summary>
    /// 
    /// </summary>
    public record Attachment(Vector3 Position, Vector3 Direction, int Type);
}
