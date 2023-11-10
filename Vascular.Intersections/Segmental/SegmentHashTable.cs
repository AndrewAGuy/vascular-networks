using System;
using System.Collections.Generic;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    /// <summary>
    /// Wraps a hash table.
    /// </summary>
    public class SegmentHashTable : SegmentRegion
    {
        private readonly AxialBoundsHashTable<Segment> segments;

        /// <summary>
        ///
        /// </summary>
        /// <param name="s"></param>
        /// <param name="stride"></param>
        /// <param name="factor"></param>
        public SegmentHashTable(IEnumerable<Segment> s, double stride = 1, double factor = 2)
        {
            segments = new(s, stride, factor);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
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

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override AxialBounds GetAxialBounds()
        {
            return segments.GetAxialBounds();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<Segment> GetEnumerator()
        {
            return segments.GetEnumerator();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        public override void Query(AxialBounds query, Action<Segment> action)
        {
            segments.Query(query, action);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public override bool Query(AxialBounds query, Func<Segment, bool> action)
        {
            return segments.Query(query, action);
        }
    }
}
