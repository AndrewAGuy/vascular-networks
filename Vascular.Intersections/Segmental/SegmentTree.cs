using System;
using System.Collections.Generic;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Intersections.Segmental
{
    /// <summary>
    /// Wraps a collection of segments in a tree.
    /// </summary>
    public class SegmentTree : SegmentRegion
    {
        private readonly AxialBoundsBinaryTreeNode<Segment> tree;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segments"></param>
        public SegmentTree(IEnumerable<Segment> segments)
        {
            tree = AxialBoundsBinaryTree.Create(segments);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public override IReadOnlyList<SegmentIntersection> Evaluate(Network network)
        {
            var intersections = new List<SegmentIntersection>();
            Test(intersections, tree, network.Root);
            return intersections;
        }

        private void Test(List<SegmentIntersection> intersections, AxialBoundsBinaryTreeNode<Segment> node, Branch branch)
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
                      
            // If local bounds of branch didn't hit global bounds of node, then it won't hit anything downstream either.
            // Split search based on child pairs if we can, otherwise we need to keep searching down the network against this.
            if (node is AxialBoundsBinaryTreeSplit<Segment> split)
            {
                foreach (var child in branch.Children)
                {
                    if (child.GlobalBounds.Intersects(split.Left.GetAxialBounds()))
                    {
                        Test(intersections, split.Left, child);
                    }
                    if (child.GlobalBounds.Intersects(split.Right.GetAxialBounds()))
                    {
                        Test(intersections, split.Right, child);
                    }
                }
            }
            else
            {
                foreach (var child in branch.Children)
                {
                    if (child.GlobalBounds.Intersects(node.GetAxialBounds()))
                    {
                        Test(intersections, node, child);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        public override void Query(AxialBounds query, Action<Segment> action)
        {
            tree.Query(query, action);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override AxialBounds GetAxialBounds()
        {
            return tree.GetAxialBounds();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<Segment> GetEnumerator()
        {
            return tree.GetEnumerator();
        }
    }
}
