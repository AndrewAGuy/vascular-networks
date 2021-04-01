using System.Collections.Generic;
using Vascular.Geometry.Generators;
using Vascular.Structure;

namespace Vascular.Intersections.Collision
{
    /// <summary>
    /// Base type for colliders.
    /// </summary>
    public abstract class Collider
    {
        /// <summary>
        /// Finds the intersections and returns them as a single list.
        /// </summary>
        /// <returns></returns>
        public abstract IReadOnlyList<SegmentIntersection> Evaluate();

        /// <summary>
        /// Used for generating normals.
        /// </summary>
        public CubeGrayCode GrayCode { get; set; } = new CubeGrayCode();

        /// <summary>
        /// Adds any intersecting segments between <paramref name="a"/> and <paramref name="b"/> to <paramref name="ii"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ii"></param>
        /// <param name="grayCode"></param>
        public static void TestSegments(Branch a, Branch b, List<SegmentIntersection> ii, CubeGrayCode grayCode)
        {
            foreach (var sA in a.Segments)
            {
                foreach (var sB in b.Segments)
                {
                    if (sA.Bounds.Intersects(sB.Bounds))
                    {
                        var i = new SegmentIntersection(sA, sB, grayCode);
                        if (i.Intersecting)
                        {
                            ii.Add(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds any intersecting segments between <paramref name="a"/> and <paramref name="b"/> to <paramref name="ii"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ii"></param>
        /// <param name="grayCode"></param>
        public static void TestSegments(IEnumerable<Segment> a, IEnumerable<Segment> b, List<SegmentIntersection> ii, CubeGrayCode grayCode)
        {
            foreach (var sA in a)
            {
                foreach (var sB in b)
                {
                    if (sA.Bounds.Intersects(sB.Bounds))
                    {
                        var i = new SegmentIntersection(sA, sB, grayCode);
                        if (i.Intersecting)
                        {
                            ii.Add(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds any intersecting segments between <paramref name="a"/> and <paramref name="b"/> to <paramref name="ii"/>.
        /// Uses the relationship <paramref name="br"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ii"></param>
        /// <param name="grayCode"></param>
        /// <param name="br"></param>
        public static void TestSegments(Branch a, Branch b, List<SegmentIntersection> ii, CubeGrayCode grayCode, BranchRelationship br)
        {
            foreach (var sA in a.Segments)
            {
                foreach (var sB in b.Segments)
                {
                    if (sA.Bounds.Intersects(sB.Bounds))
                    {
                        var i = new SegmentIntersection(sA, sB, grayCode, br: br);
                        if (i.Intersecting)
                        {
                            ii.Add(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds any intersecting segments between <paramref name="a"/> and <paramref name="b"/> to <paramref name="ii"/>.
        /// Uses the relationship <paramref name="br"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ii"></param>
        /// <param name="grayCode"></param>
        /// <param name="br"></param>
        public static void TestSegments(IEnumerable<Segment> a, IEnumerable<Segment> b, List<SegmentIntersection> ii,
            CubeGrayCode grayCode, BranchRelationship br)
        {
            foreach (var sA in a)
            {
                foreach (var sB in b)
                {
                    if (sA.Bounds.Intersects(sB.Bounds))
                    {
                        var i = new SegmentIntersection(sA, sB, grayCode, br: br);
                        if (i.Intersecting)
                        {
                            ii.Add(i);
                        }
                    }
                }
            }
        }
    }
}
