using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Generators;
using Vascular.Structure;

namespace Vascular.Intersections.Collision
{
    public class DisjointCollider : Collider
    {
        private readonly Network networkA;
        private readonly Network networkB;
        private List<SegmentIntersection> intersections = null;

        public DisjointCollider(Network root1, Network root2)
        {
            networkA = root1;
            networkB = root2;
        }

        private void SearchDownstream(Branch root, Branch check)
        {
            if (root.LocalBounds.Intersects(check.LocalBounds))
            {
                // Test all segments in these branches
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

        private void Search(Branch a, Branch v)
        {
            // Work down tree b against elements of tree a
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

        public override IReadOnlyList<SegmentIntersection> Evaluate()
        {
            intersections = new List<SegmentIntersection>();
            Search(networkA.Root, networkB.Root);
            return intersections;
        }
    }
}
