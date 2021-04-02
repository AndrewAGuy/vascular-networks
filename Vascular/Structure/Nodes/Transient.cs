using System.Runtime.Serialization;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// Links segments in a branch.
    /// </summary>
    [DataContract]
    public class Transient : IMobileNode
    {
        /// <inheritdoc/>
        [DataMember]
        public Segment Parent { get; set; } = null;

        [DataMember]
        private Segment child = null;

        /// <summary>
        /// Access to the child branch. Updates <see cref="Children"/> when set.
        /// </summary>
        public Segment Child
        {
            get => child;
            set
            {
                child = value;
                this.Children[0] = value;
            }
        }

        /// <inheritdoc/>
        [DataMember]
        public Segment[] Children { get; } = new Segment[1] { null };

        /// <inheritdoc/>
        [DataMember]
        public Vector3 Position { get; set; } = null;

        /// <inheritdoc/>
        public void UpdatePhysicalAndPropagate()
        {
            this.Parent.UpdateLength();
            child.UpdateLength();
            child.Branch.UpdatePhysicalLocal();
            child.Branch.PropagatePhysicalUpstream();
        }
    }
}
