using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Generators;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    public abstract class SegmentRegion : IIntersectionEvaluator<SegmentIntersection>
    {
        public CubeGrayCode GrayCode { get; set; } = new CubeGrayCode();

        // Convention of having segment B being the network segment, segment A being the forbidden segment.
        public abstract IEnumerable<SegmentIntersection> Evaluate(Network network);
    }
}
