using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Geometry.Triangulation;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    /// <summary>
    /// Wraps a mesh in a binary tree an implements a query pattern similar to the network collision detectors.
    /// </summary>
    public class MeshTree : IIntersectionEvaluator<TriangleIntersection>
    {
        /// <summary>
        ///
        /// </summary>
        public AxialBoundsBinaryTreeNode<TriangleSurfaceTest> Tree { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tree"></param>
        public MeshTree(AxialBoundsBinaryTreeNode<TriangleSurfaceTest> tree)
        {
            this.Tree = tree;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="r"></param>
        /// <param name="t2"></param>
        public MeshTree(Mesh mesh, double r = 0.0, double t2 = 1e-12)
        {
            var surfaceTests = mesh.T.Select(triangle => new TriangleSurfaceTest(triangle, r, t2)).ToList();
            this.Tree = AxialBoundsBinaryTree.Create(surfaceTests);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mesh"></param>
        public MeshTree(IEnumerable<TriangleSurfaceTest> mesh)
        {
            this.Tree = AxialBoundsBinaryTree.Create(mesh);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public IEnumerable<TriangleIntersection> Evaluate(Network network)
        {
            var intersections = new List<TriangleIntersection>();
            if (network.Root != null)
            {
                Test(intersections, this.Tree, network.Root);
            }
            return intersections;
        }

        private static void Test(List<TriangleIntersection> intersections,
            AxialBoundsBinaryTreeNode<TriangleSurfaceTest> node, Branch root)
        {
            node.Query(root, (branch, triangle) =>
            {
                branch.Query(triangle.GetAxialBounds(), segment =>
                {
                    double fraction = 0;
                    Vector3? position = null;
                    if (triangle.TestRay(segment.Start.Position, segment.Direction, segment.Radius,
                        ref fraction, ref position))
                    {
                        intersections.Add(new TriangleIntersection(segment, triangle, fraction));
                    }
                });
            });
        }
    }
}
