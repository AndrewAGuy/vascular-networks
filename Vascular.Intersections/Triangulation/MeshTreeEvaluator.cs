using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Geometry.Triangulation;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    public class TreeEvaluator : IIntersectionEvaluator<TriangleIntersection>
    {
        public AxialBoundsBinaryTreeNode<TriangleSurfaceTest> Tree { get; }

        public TreeEvaluator(AxialBoundsBinaryTreeNode<TriangleSurfaceTest> tree)
        {
            this.Tree = tree;
        }

        public TreeEvaluator(Mesh mesh, double r = 0.0, double t2 = 1e-12)
        {
            var surfaceTests = mesh.T.Select(triangle => new TriangleSurfaceTest(triangle, r, t2)).ToList();
            this.Tree = AxialBoundsBinaryTree.Create(surfaceTests);
        }

        public IEnumerable<TriangleIntersection> Evaluate(Network network)
        {
            var intersections = new List<TriangleIntersection>();
            if (network.Root != null)
            {
                Test(intersections, this.Tree, network.Root);
            }
            return intersections;
        }

        private static void Test(List<TriangleIntersection> intersections, AxialBoundsBinaryTreeNode<TriangleSurfaceTest> node, Branch branch)
        {
            if (branch.LocalBounds.Intersects(node.GetAxialBounds()))
            {
                // We want to test this branch against everything downstream of this node.
                // We will never test this branch against anything again.
                node.Query(branch.LocalBounds, triangle =>
                {
                    branch.Query(triangle.GetAxialBounds(), networkSegment =>
                    {
                        double fraction = 0;
                        Vector3 position = null;
                        if (triangle.TestRay(networkSegment.Start.Position, networkSegment.Direction, networkSegment.Radius, ref fraction, ref position))
                        {
                            intersections.Add(new TriangleIntersection(networkSegment, triangle, fraction));
                        }
                    });
                });
            }
            // If local bounds of branch didn't hit global bounds of node, then it won't hit anything downstream either.
            // Split search based on child pairs if we can, otherwise we need to keep searching down the network against this.
            if (node is AxialBoundsBinaryTreeSplit<TriangleSurfaceTest> split)
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
    }
}
