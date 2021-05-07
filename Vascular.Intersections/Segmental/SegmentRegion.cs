using System;
using System.Collections;
using System.Collections.Generic;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Generators;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    /// <summary>
    /// Represents a region of space forbidden to networks, defined by segments for easier resolution.
    /// </summary>
    public abstract class SegmentRegion : IIntersectionEvaluator<SegmentIntersection>, IAxialBoundsQueryable<Segment>
    {
        /// <summary>
        /// For generating normals.
        /// </summary>
        public CubeGrayCode GrayCode { get; set; } = new CubeGrayCode();

        /// <summary>
        /// Convention of having segment B being the network segment, segment A being the forbidden segment.
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public abstract IEnumerable<SegmentIntersection> Evaluate(Network network);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract AxialBounds GetAxialBounds();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerator<Segment> GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        public abstract void Query(AxialBounds query, Action<Segment> action);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
