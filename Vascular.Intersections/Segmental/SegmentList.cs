using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    public class SegmentList : SegmentRegion
    {
        private readonly List<Segment> list = new List<Segment>();

        public SegmentList(IEnumerable<Segment> segments)
        {
            list.AddRange(segments);
        }

        public void Add(Segment segment)
        {
            list.Add(segment);
        }

        public override IReadOnlyList<SegmentIntersection> Evaluate(Network network)
        {
            var intersections = new List<SegmentIntersection>();
            foreach (var forbiddenSegment in list)
            {
                network.Query(forbiddenSegment.Bounds, networkSegment =>
                {
                    var i = new SegmentIntersection(forbiddenSegment, networkSegment, this.GrayCode);
                    if (i.Intersecting)
                    {
                        intersections.Add(i);
                    }
                });
            }
            return intersections;
        }

        public AxialBounds GetAxialBounds()
        {
            return list.GetTotalBounds();
        }

        public void Query(AxialBounds query, Action<Segment> action)
        {
            foreach (var s in list)
            {
                if (query.Intersects(s.Bounds))
                {
                    action(s);
                }
            }
        }
    }
}
