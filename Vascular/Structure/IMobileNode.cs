using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure
{
    public interface IMobileNode : INode
    {
        void UpdatePhysicalAndPropagate();
    }
}
