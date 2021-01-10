using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Generators;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    public abstract class SegmentRegion
    {
        public CubeGrayCode GrayCode { get; set; } = new CubeGrayCode();

        // Convention of having segment B being the network segment, segment A being the forbidden segment.
        public abstract IReadOnlyList<SegmentIntersection> Evaluate(Network network);
    }
}
