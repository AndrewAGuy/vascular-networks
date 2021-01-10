using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    [DataContract]
    public class Dummy : INode
    {
        public Segment Parent
        {
            get
            {
                return null;
            }
            set
            {
                return;
            }
        }

        public Segment[] Children
        {
            get
            {
                return null;
            }
        }

        [DataMember]
        public Vector3 Position { get; set; } = null;
    }
}
