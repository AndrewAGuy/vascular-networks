using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    [Serializable]
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

        public Vector3 Position { get; set; } = null;
    }
}
