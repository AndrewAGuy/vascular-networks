using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Generators;
using Vascular.Structure;

namespace Vascular.Intersections.Collision
{
    public abstract class Collider
    {
        public abstract IReadOnlyList<SegmentIntersection> Evaluate();
        public CubeGrayCode GrayCode { get; set; } = new CubeGrayCode();

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
