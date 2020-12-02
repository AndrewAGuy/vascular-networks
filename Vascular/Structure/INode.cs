using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Structure
{
    public interface INode
    {
        Segment Parent { get; set; }

        Segment[] Children { get; }

        Vector3 Position { get; set; }
    }
}
