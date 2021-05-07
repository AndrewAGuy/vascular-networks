using System.Runtime.Serialization;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// For using <see cref="Segment"/> instances for non-tree things.
    /// </summary>
    [DataContract]
    public class Dummy : INode
    {
        /// <inheritdoc/>
        public Segment Parent
        {
            get => null;
            set { }
        }

        /// <inheritdoc/>
        public Segment[] Children => null;

        /// <inheritdoc/>
        [DataMember]
        public Vector3 Position { get; set; } = null;
    }
}
