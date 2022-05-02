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
    /// <summary>
    /// 
    /// </summary>
    public class CapillaryLattice : CapillaryBase
    {
        /// <summary>
        /// 
        /// </summary>
        public Lattice Lattice { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Vector3, bool> PermittedVertex { get; set; } = x => true;

        /// <summary>
        /// 
        /// </summary>
        public Func<Vector3, Vector3, bool> PermittedEdge { get; set; } = (x, y) => true;

        /// <summary>
        /// 
        /// </summary>
        public Func<Vector3, Vector3, double> RadiusFactor { get; set; } = (x, y) => 1;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override double GetRadius(Edge e)
        {
            return this.Radius * this.RadiusFactor(e.S.P, e.E.P);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public override (Graph<Vertex, Edge>, HashSet<Vector3>) GenerateChunkEdges(AxialBounds bounds)
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
            var dG = new HashSet<Vector3>(faceVertices);

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
                                    g.AddEdge(x0, x);
                                }
                            }
                            else
                            {
                                dG.Add(x0);
                            }
                        }
                    }
                }
            }

            return (g, dG);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="existingBoundary"></param>
        /// <param name="adding"></param>
        /// <param name="addingBoundary"></param>
        /// <param name="vessels"></param>
        public override void StitchChunk(Graph<Vertex, Edge> existing, HashSet<Vector3> existingBoundary,
            Graph<Vertex, Edge> adding, HashSet<Vector3> addingBoundary,
            IEnumerable<IAxialBoundsQueryable<Segment>> vessels)
        {
            Merge(existing, adding);

            var C = this.Lattice.VoronoiCell.Connections;
            var bounds = new AxialBounds();
            foreach (var x0 in addingBoundary)
            {
                // Should not be possible to have vertices present in both boundaries, as we use strict inequality
                // at the upper boundary and all vertex positions have followed the same transform from integral
                // coordinates, so will have been generated at the exact same values.
                var z0 = this.Lattice.ClosestVectorBasis(x0);
                foreach (var c in C)
                {
                    var z1 = z0 + c;
                    var x1 = this.Lattice.ToSpace(z1);
                    if (!existingBoundary.Contains(x1) || 
                        !this.PermittedEdge(x0, x1))
                    {
                        continue;
                    }

                    var edge = existing.AddEdge(x0, x1);
                    bounds.Append(new AxialBounds(x0, x1, GetRadius(edge)));
                }
            }

            var segments = new List<Segment>();
            foreach (var n in vessels)
            {
                n.Query(bounds, seg => segments.Add(seg));
            }
            var segTree = AxialBoundsBinaryTree.Create(segments.Select(seg => new SegmentSurfaceTest(seg)));
            RemoveIllegalIntersections(existing, segTree);
        }

        private (Vertex, double) Walk(
            Graph<Vertex, Edge> graph, Vertex start, Vector3 direction, HashSet<Edge> visited)
        {
            var lattice = this.Lattice;
            var (vertex, radius) = (start, 0.0);
            var index = lattice.ClosestVectorBasis(start.P);

            while (true)
            {
                // Does the next vertex in this direction exist?
                index += direction;
                var xEnd = lattice.ToSpace(index);
                if (!graph.V.TryGetValue(xEnd, out var vEnd))
                {
                    break;
                }

                // Does the edge exist, and has it been processed already?
                var edgeTest = new Edge(vertex, vEnd);
                if (!graph.E.TryGetValue(edgeTest, out var edge) ||
                    visited.Contains(edge))
                {
                    break;
                }

                // Does the edge have the same radius, if not the first edge?
                var rEdge = GetRadius(edge);
                if (vertex == start)
                {
                    radius = rEdge;
                }
                else if (rEdge != radius)
                {
                    break;
                }

                // Update and progress to next vertex
                visited.Add(edge);
                vertex = vEnd;
            }

            return (vertex, radius);
        }

        /// <summary>
        /// Take edges from <paramref name="graph"/> with the same radius and alignment
        /// and combine them into single segments.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public IEnumerable<Segment> Merge(Graph<Vertex, Edge> graph)
        {
            var lattice = this.Lattice;
            var visited = new HashSet<Edge>(graph.E.Count);
            var C = lattice.VoronoiCell.Connections;

            foreach (var v in graph.V.Values)
            {
                var index = lattice.ClosestVectorBasis(v.P);
                foreach (var c in C)
                {
                    var (vP, rP) = Walk(graph, v, c, visited);
                    var (vN, rN) = Walk(graph, v, -c, visited);

                    if (rP > 0)
                    {
                        if (rP == rN)
                        {
                            yield return Segment.MakeDummy(vN.P, vP.P, rP);
                            continue;
                        }
                        else
                        {
                            yield return Segment.MakeDummy(v.P, vP.P, rP);
                        }
                    }

                    if (rN > 0)
                    {
                        yield return Segment.MakeDummy(v.P, vN.P, rN);
                    }
                }
            }
        }
    }
}
