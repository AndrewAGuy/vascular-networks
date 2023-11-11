using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Intersections.Collision
{
    /// <summary>
    /// Tests for intersections allowing terminals which have non-empty
    /// <see cref="Terminal.Partners"/> to have intersections at their matching points.
    /// </summary>
    public class MatchedCollider : Collider
    {
        private readonly Network networkA;
        private readonly Network networkB;
        private List<SegmentIntersection> intersections = null!;
        private readonly List<Branch> ignore = new();

        /// <summary>
        /// If true, reduce the immune set to just the segments until divergence.
        /// </summary>
        public bool ImmuneSetContraction { get; set; } = true;

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public MatchedCollider(Network a, Network b)
        {
            networkA = a;
            networkB = b;
        }

        private void SearchDownstream(Branch root, Branch check)
        {
            if (!ignore.Contains(root) && root.LocalBounds.Intersects(check.LocalBounds))
            {
                TestSegments(root, check, intersections, this.GrayCode, BranchRelationship.Disjoint);
            }
            foreach (var child in root.Children)
            {
                if (child.GlobalBounds.Intersects(check.LocalBounds))
                {
                    SearchDownstream(child, check);
                }
            }
        }

        private void SetIgnore(Branch b)
        {
            ignore.Clear();
            if (b.End is Terminal t && t.Partners is not null)
            {
                foreach (var p in t.Partners)
                {
                    ignore.Add(p.Upstream);
                }
                if (this.ImmuneSetContraction)
                {
                    TestNonIgnored(t.Partners);
                }
            }
        }

        private void Search(Branch a, Branch v)
        {
            // Work down tree b against elements of tree a
            SetIgnore(a);
            SearchDownstream(v, a);
            foreach (var ca in a.Children)
            {
                if (ca.GlobalBounds.Intersects(v.LocalBounds))
                {
                    Search(ca, v);
                }
                else
                {
                    foreach (var cb in v.Children)
                    {
                        if (ca.GlobalBounds.Intersects(cb.GlobalBounds))
                        {
                            Search(ca, cb);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override IReadOnlyList<SegmentIntersection> Evaluate()
        {
            intersections = new();
            Search(networkA.Root, networkB.Root);
            return intersections;
        }

        private void TestNonIgnored(Terminal[] T)
        {
            var nonIgnored = new List<List<Segment>>(T.Length);
            var R = 0.0;
            foreach (var t in T)
            {
                var r = t.Parent.Radius;
                if (r > R)
                {
                    R = r;
                }
            }
            foreach (var t in T)
            {
                nonIgnored.Add(GetNonIgnored(t, R));
            }
            for (var a = 0; a < nonIgnored.Count; ++a)
            {
                for (var b = a + 1; b < nonIgnored.Count; ++b)
                {
                    TestSegments(nonIgnored[a], nonIgnored[b], intersections, this.GrayCode, BranchRelationship.Matched);
                }
            }
        }

        private static List<Segment> GetNonIgnored(Terminal t, double R)
        {
            var S = t.Upstream.Segments;
            var segs = new List<Segment>(S.Count);
            var curSeg = S[S.Count - 1];
            while (true)
            {
                if (curSeg.Start is Transient tr)
                {
                    curSeg = tr.Parent;
                    var d2 = Vector3.DistanceSquared(t.Position, curSeg.End.Position);
                    var r2 = Math.Pow(curSeg.Radius + R, 2);
                    if (d2 > r2)
                    {
                        break;
                    }
                }
                else
                {
                    return segs;
                }
            }
            while (true)
            {
                segs.Add(curSeg);
                if (curSeg.Start is Transient tr)
                {
                    curSeg = tr.Parent;
                }
                else
                {
                    return segs;
                }
            }
        }
    }
}
