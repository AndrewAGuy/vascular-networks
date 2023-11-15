using System;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// For using <see cref="Segment"/> instances for non-tree things.
    /// </summary>
    public class Dummy : INode
    {
        /// <inheritdoc/>
        public Segment? Parent
        {
            get => null;
            set => throw new TopologyException("Dummy node can not have parent");
        }

        /// <inheritdoc/>
        public Segment[] Children => Array.Empty<Segment>();

        /// <inheritdoc/>
        public Vector3 Position { get; set; } = Vector3.INVALID;
    }
}
