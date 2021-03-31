using System.Runtime.Serialization;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    [DataContract]
    public class Dummy : INode
    {
        public Segment Parent
        {
            get => null;
            set { }
        }

        public Segment[] Children => null;

        [DataMember]
        public Vector3 Position { get; set; } = null;
    }
}
