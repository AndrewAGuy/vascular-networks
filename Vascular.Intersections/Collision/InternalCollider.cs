using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Intersections.Collision
{
    public class InternalCollider : Collider
    {
        private readonly Network network;
        private List<SegmentIntersection> intersections = null;
        private bool testNonImmune = true;
        public bool ImmuneSetContraction
        {
            get => testNonImmune;
            set => testNonImmune = value;
        }

        public InternalCollider(Network nRoot)
        {
            network = nRoot;
        }

        private BranchRelationship activeRelationship = BranchRelationship.Internal;

        private void TestFrom(Branch from)
        {
            // From this branch: Work out the immune set bounds.
            var immuneBounds = ImmuneBounds(from);
            // Test this branch against all branches and segments downstream of this bounds.
            // Set relationship to capture upstream/downstream relation, reset after.
            activeRelationship = BranchRelationship.Upstream;
            foreach (var lastImmune in immuneBounds)
            {
                foreach (var down in lastImmune.Downstream)
                {
                    TestDownstream(from, down);
                }
            }
            activeRelationship = BranchRelationship.Internal;
            // Test each downstream section against each other.
            for (var i = 0; i < immuneBounds.Count; ++i)
            {
                for (var j = i + 1; j < immuneBounds.Count; ++j)
                {
                    foreach (var downA in immuneBounds[i].Downstream)
                    {
                        foreach (var downB in immuneBounds[j].Downstream)
                        {
                            TestDisjoint(downA, downB);
                        }
                    }
                }
            }
            // Test the non-immune internals
            if (testNonImmune && immuneBounds.Count > 1)
            {
                TestNonImmune(immuneBounds, from);
            }
            // Now move down the tree
            foreach (var child in from.Children)
            {
                TestFrom(child);
            }
        }

        private void TestDisjoint(Branch a, Branch b)
        {
            // Test a against everything downstream of b
            TestDownstream(a, b);
            // Need not test a again
            foreach (var downA in a.Children)
            {
                if (downA.GlobalBounds.Intersects(b.LocalBounds))
                {
                    TestDisjoint(downA, b);
                }
                else
                {
                    foreach (var downB in b.Children)
                    {
                        if (downA.GlobalBounds.Intersects(downB.GlobalBounds))
                        {
                            TestDisjoint(downA, downB);
                        }
                    }
                }
            }
        }

        private void TestDownstream(Branch test, Branch down)
        {
            if (test.LocalBounds.Intersects(down.LocalBounds))
            {
                TestSegments(test, down, intersections, this.GrayCode, activeRelationship);
            }
            foreach (var child in down.Children)
            {
                if (test.LocalBounds.Intersects(child.GlobalBounds))
                {
                    TestDownstream(test, child);
                }
            }
        }

        public static List<BranchNode> ImmuneBounds(Branch from)
        {
            var firstDiverged = new List<BranchNode>();
            ExtendImmune(from, from.End, firstDiverged);
            return firstDiverged;
        }

        private static void ExtendImmune(Branch from, BranchNode test, List<BranchNode> immune)
        {
            foreach (var down in test.Downstream)
            {
                var dist2 = Vector3.DistanceSquared(from.End.Position, down.End.Position);
                var rsum2 = Math.Pow(from.Radius + down.Radius, 2);
                if (dist2 <= rsum2)
                {
                    ExtendImmune(from, down.End, immune);
                }
                else
                {
                    immune.Add(down.End);
                }
            }
        }

        public override IReadOnlyList<SegmentIntersection> Evaluate()
        {
            intersections = new List<SegmentIntersection>();
            TestFrom(network.Root);
            return intersections;
        }

        private void TestNonImmune(List<BranchNode> immuneBounds, Branch from)
        {
            var nonImmune = new List<List<Segment>>(immuneBounds.Count);
            foreach (var i in immuneBounds)
            {
                nonImmune.Add(NonImmuneSegments(from, i));
            }
            for (var a = 0; a < nonImmune.Count; ++a)
            {
                // Test against branch splitting from
                var A = nonImmune[a];
                TestSegments(A, from.Segments, intersections, this.GrayCode, BranchRelationship.Downstream);
                // Test against each other
                for (var b = a + 1; b < nonImmune.Count; ++b)
                {
                    TestSegments(A, nonImmune[b], intersections, this.GrayCode, BranchRelationship.Internal);
                }
                // Test against all downstream (not in this branch though!)
                var bounds = A.GetTotalBounds();
                if (bounds != null)
                {
                    for (var i = 0; i < immuneBounds.Count; ++i)
                    {
                        if (i != a)
                        {
                            foreach (var d in immuneBounds[i].Downstream)
                            {
                                if (d.GlobalBounds.Intersects(bounds))
                                {
                                    Network.BranchQuery(bounds, b => TestSegments(A, b.Segments, intersections, 
                                        this.GrayCode, BranchRelationship.Internal), d);
                                }
                            }
                        }
                    }
                }
            }

        }

        private static List<Segment> NonImmuneSegments(Branch from, BranchNode end)
        {
            var S = end.Upstream.Segments;
            var segs = new List<Segment>(S.Count);
            var curSeg = S[S.Count - 1];
            while (true)
            {
                var dist2 = Vector3.DistanceSquared(from.End.Position, curSeg.Start.Position);
                var rsum2 = Math.Pow(from.Radius + curSeg.Radius, 2);
                if (dist2 > rsum2 && curSeg.Start is Transient tr)
                {
                    segs.Add(curSeg);
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
