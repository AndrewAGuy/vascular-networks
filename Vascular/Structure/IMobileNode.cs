using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Structure.Nodes;

namespace Vascular.Structure
{
    public interface IMobileNode : INode
    {
        void UpdatePhysicalAndPropagate();
    }
}
