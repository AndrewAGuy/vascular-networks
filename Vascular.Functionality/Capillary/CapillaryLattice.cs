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
                        if (!bounds.Intersects(x0) ||
                            !this.PermittedVertex(x0))
                        {
                            continue;
                        }
                        foreach (var c in C)
                        {
                            var z = z0 + c;
                            var x = this.Lattice.ToSpace(z);
                            if (bounds.Intersects(x))
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
                // Possible that vertex exists in both, as bounds checking uses weak comparison
                if (existing.V.TryGetValue(ab, out var ev))
                {
                    
                }
                
            }
        }
    }
}
