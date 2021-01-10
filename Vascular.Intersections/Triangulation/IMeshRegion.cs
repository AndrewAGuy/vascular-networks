using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    public interface IMeshRegion
    {
        IReadOnlyList<TriangleIntersection> Evaluate(Network network);
    }
}
