using System;
using System.Collections.Generic;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    /// <summary>
    /// The most simple collection, best for small numbers such as defining a few voids or a cylinder core.
    /// </summary>
    public class SegmentList : SegmentRegion
    {
        private readonly List<Segment> list = new List<Segment>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segments"></param>
        public SegmentList(IEnumerable<Segment> segments)
        {
            list.AddRange(segments);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        public void Add(Segment segment)
        {
            list.Add(segment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override AxialBounds GetAxialBounds()
        {
            return list.GetTotalBounds();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        public override void Query(AxialBounds query, Action<Segment> action)
        {
            foreach (var s in list)
            {
                if (query.Intersects(s.Bounds))
                {
                    action(s);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<Segment> GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
