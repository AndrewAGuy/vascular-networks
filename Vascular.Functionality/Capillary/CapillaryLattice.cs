using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Graphs;
using Vascular.Geometry.Lattices;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Functionality.Capillary
{
    public class CapillaryLattice : CapillaryBase
    {
        public Lattice Lattice { get; set; }

        public Func<Vector3, bool> PermittedVertex { get; set; } = x => true;

        public Func<Vector3, Vector3, bool> PermittedEdge { get; set; } = (x, y) => true;

        public override (Graph<Vertex, Edge>, HashSet<Vector3>) GenerateChunk(AxialBounds bounds)
        {
            // Call with the bounds splitting halfway along the edges
            // Estimate number of points from volume and connectivity
            var r = bounds.Range;
            var nv = r.Product / this.Lattice.Determinant;
            var C = this.Lattice.VoronoiCell.Connections;
            var ne = nv * C.Length * 0.5;
            var g = new Graph((int)nv, (int)ne);

            // Now get chunk coordinates
            var bv = bounds
                .Vertices()
                .Select(this.Lattice.ToBasis)
                .GetTotalBounds();
            var (iMin, jMin, kMin) = bv.Lower.Floor;
            var (iMax, jMax, kMax) = bv.Upper.Ceiling;

            // Prepare to extract vertices that touch the face - estimate as though it's a cube
            var faceVertices = 2 * (
                (iMax - iMin) * (jMax - jMin) +
                (jMax - jMin) * (kMax - kMin) +
                (kMax - kMin) * (iMax - iMin));
            var chunkExterior = new HashSet<Vector3>(faceVertices);

            // Loop and create edges
            for (var i = iMin; i <= iMax; ++i)
            {
                for (var j = jMin; j <= jMax; ++j)
                {
                    for (var k = kMin; k <= kMax; ++k)
                    {
                        var z0 = new Vector3(i, j, k);
                        var x0 = this.Lattice.ToSpace(z0);
                        if (!bounds.IntersectsOpenUpper(x0) ||
                            !this.PermittedVertex(x0))
                        {
                            continue;
                        }
                        foreach (var c in C)
                        {
                            var z = z0 + c;
                            var x = this.Lattice.ToSpace(z);
                            if (bounds.IntersectsOpenUpper(x))
                            {
                                if (this.PermittedEdge(x0, x))
                                {
                                    var v0 = g.AddVertex(x0);
                                    var v = g.AddVertex(x);
                                    var e = new Edge(v0, v);
                                    g.AddEdge(e);
                                }
                            }
                            else
                            {
                                chunkExterior.Add(x0);
                            }
                        }
                    }
                }
            }

            // Identify edges that intersect major vessels, or intersect minor vessels by an insufficient amount
            var segments = new List<Segment>();
            foreach (var n in this.Networks)
            {
                n.Query(bounds, seg => segments.Add(seg));
            }
            var segTree = AxialBoundsBinaryTree.Create(segments.Select(seg => new SegmentSurfaceTest(seg)));
            Continuous.RemoveIllegalIntersections(g, segTree, this);

            // Remove leaf branches, except for those that are at the chunk boundary
            g.RemoveLeafBranches(v => chunkExterior.Contains(v.P));

            return (g, chunkExterior);
        }

        protected override void StitchChunks(Graph<Vertex, Edge> existing, HashSet<Vector3> existingBoundary,
            Graph<Vertex, Edge> adding, HashSet<Vector3> addingBoundary)
        {
            // Find vertices that are on the boundary faces and merge them
            foreach (var ab in addingBoundary)
            {
                // Should not be possible to have vertices present in both boundaries, as we use strict inequality
                // at the upper boundary and all vertex positions have followed the same transform from integral
                // coordinates, so will have been generated at the exact same values.

            }
        }

        public static IEnumerable<Segment> ConvertMerging(Graph<Vertex, Edge> graph, Lattice lattice, Func<Edge, Segment> convert)
        {
            var visited = new HashSet<Edge>(graph.E.Count);
            var C = lattice.VoronoiCell.Connections;

            Vertex walkDirection(Vector3 startIndex, Vector3 directionIndex)
            {
                var xStart = lattice.ToSpace(startIndex);
                if (!graph.V.TryGetValue(xStart, out var vStart))
                {
                    return null;
                }

                var index = startIndex;
                while (true)
                {
                    // Firstly, does the next vertex in this direction exist?
                    index += directionIndex;
                    var xEnd = lattice.ToSpace(index);
                    if (!graph.V.TryGetValue(xEnd, out var vEnd))
                    {
                        return vStart;
                    }
                    // Does the edge exist?
                    var edgeTest = new Edge(vStart, vEnd);
                    if (!graph.E.TryGetValue(edgeTest, out var edge))
                    {
                        return vStart;
                    }
                    // Has it been visited already?
                    if (visited.Contains(edge))
                    {
                        return null;
                    }
                    else
                    {
                        // Update and progress to next vertex
                        visited.Add(edge);
                        vStart = vEnd;
                    }
                }
            }

            foreach (var v in graph.V.Values)
            {
                var index = lattice.ClosestVectorBasis(v.P);
                // Rather inefficiently, test each direction both forwards and back.
                // TODO: perhaps force lattice directions to be such that the second half is the first in reverse?
                foreach (var c in C)
                {
                    var vP = walkDirection(index, c);
                    var vN = walkDirection(index, -c);
                    if (vP != null && vN != null && vP != vN)
                    {
                        yield return convert(new Edge(vN, vP));
                    }
                }
            }
        }
    }
}
