using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    [DataContract]
    public class Transient : IMobileNode
    {
        [DataMember]
        public Segment Parent { get; set; } = null;

        [DataMember]
        private Segment child = null;

        public Segment Child
        {
            get
            {
                return child;
            }
            set
            {
                child = value;
                this.Children[0] = value;
            }
        }

        [DataMember]
        public Segment[] Children { get; } = new Segment[1] { null };

        [DataMember]
        public Vector3 Position { get; set; } = null;

        public void UpdatePhysicalAndPropagate()
        {
            this.Parent.UpdateLength();
            child.UpdateLength();
            child.Branch.UpdatePhysicalLocal();
            child.Branch.PropagatePhysicalUpstream();
        }
    }
}
