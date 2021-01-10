using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    public class SegmentHashTable : SegmentRegion
    {
        private readonly AxialBoundsHashTable<Segment> segments;

        public SegmentHashTable(IEnumerable<Segment> s, double stride = 1, double factor = 2)
        {
            segments = new AxialBoundsHashTable<Segment>(s, stride, factor);
        }

        public override IReadOnlyList<SegmentIntersection> Evaluate(Network network)
        {
            var intersections = new List<SegmentIntersection>();
            foreach (var segment in network.Segments)
            {
                segments.Query(segment.Bounds, found =>
                {
                    var i = new SegmentIntersection(found, segment, this.GrayCode);
                    if (i.Intersecting)
                    {
                        intersections.Add(i);
                    }
                });
            }
            return intersections;
        }
    }
}
