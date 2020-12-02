using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    [Serializable]
    public class Transient : IMobileNode
    {
        public Segment Parent { get; set; } = null;

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

        public Segment[] Children { get; } = new Segment[1] { null };

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
