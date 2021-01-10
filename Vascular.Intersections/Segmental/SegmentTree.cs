using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    public class SegmentTree : SegmentRegion
    {
        private readonly AxialBoundsBinaryTreeNode<Segment> tree;

        public SegmentTree(IEnumerable<Segment> segments)
        {
            tree = AxialBoundsBinaryTreeNode<Segment>.Create(segments);
        }

        private List<SegmentIntersection> intersections = new List<SegmentIntersection>();

        public override IReadOnlyList<SegmentIntersection> Evaluate(Network network)
        {
            intersections.Clear();
            Test(tree, network.Root);
            return intersections;
        }

        private void Test(AxialBoundsBinaryTreeNode<Segment> node, Branch branch)
        {
            if (branch.LocalBounds.Intersects(node.GetAxialBounds()))
            {
                // We want to test this branch against everything downstream of this node.
                // We will never test this branch against anything again.
                node.Query(branch.LocalBounds, forbiddenSegment =>
                {
                    branch.Query(forbiddenSegment.Bounds, networkSegment =>
                    {
                        var i = new SegmentIntersection(forbiddenSegment, networkSegment, this.GrayCode);
                        if (i.Intersecting)
                        {
                            intersections.Add(i);
                        }
                    });
                });
            }
            else
            {
                // If local bounds of branch didn't hit global bounds of node, then it won't hit anything downstream either.
                // Split search based on child pairs if we can, otherwise we need to keep searching down the network against this.
                if (node is AxialBoundsBinaryTreeSplit<Segment> split)
                {
                    foreach (var child in branch.Children)
                    {
                        if (child.GlobalBounds.Intersects(split.Left.GetAxialBounds()))
                        {
                            Test(split.Left, child);
                        }
                        if (child.GlobalBounds.Intersects(split.Right.GetAxialBounds()))
                        {
                            Test(split.Right, child);
                        }
                    }
                }
                else
                {
                    foreach (var child in branch.Children)
                    {
                        if (child.GlobalBounds.Intersects(node.GetAxialBounds()))
                        {
                            Test(node, child);
                        }
                    }
                }
            }
        }

        public void Query(AxialBounds query, Action<Segment> action)
        {
            tree.Query(query, action);
        }

        public AxialBounds GetAxialBounds()
        {
            return tree.GetAxialBounds();
        }
    }
}
