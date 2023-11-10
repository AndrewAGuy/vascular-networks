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

        private void Test(List<SegmentIntersection> intersections,
            AxialBoundsBinaryTreeNode<Segment> node, Branch root)
        {
            node.Query(root, (branch, forbidden) =>
            {
                branch.Query(forbidden.Bounds, segment =>
                {
                    var i = new SegmentIntersection(forbidden, segment, this.GrayCode);
                    if (i.Intersecting)
                    {
                        intersections.Add(i);
                    }
                });
            });
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public override bool Query(AxialBounds query, Func<Segment, bool> action)
        {
            return tree.Query(query, action);
        }
    }
}
