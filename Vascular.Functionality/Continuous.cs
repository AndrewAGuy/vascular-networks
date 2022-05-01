using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Graphs;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Functionality
{
    /// <summary>
    /// Represents a continuous functional structure that attaches at many points to the networks.
    /// </summary>
    /// <typeparam name="TV"></typeparam>
    /// <typeparam name="TE"></typeparam>
    public abstract class Continuous<TV, TE>
        where TV : Vertex<TV, TE>, new()
        where TE : Edge<TV, TE>, new()
    {
        /// <summary>
        /// Create all vessels within a chunk.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public abstract (Graph<TV, TE> graph, HashSet<Vector3> boundary) GenerateChunkEdges(AxialBounds bounds);

        /// <summary>
        /// Generate edges, then remove illegal intersections and all leaf branches which cannot be fixed.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="vessels"></param>
        /// <returns></returns>
        public virtual (Graph<TV, TE> graph, HashSet<Vector3> boundary) GenerateChunk(
            AxialBounds bounds, IEnumerable<IAxialBoundsQueryable<Segment>> vessels)
        {
            var (g, chunkExterior) = GenerateChunkEdges(bounds);

            // Identify edges that intersect major vessels, or intersect minor vessels by an insufficient amount
            var segments = new List<Segment>();
            foreach (var n in vessels)
            {
                n.Query(bounds, seg => segments.Add(seg));
            }
            var segTree = AxialBoundsBinaryTree.Create(segments.Select(seg => new SegmentSurfaceTest(seg)));
            Continuous.RemoveIllegalIntersections(g, segTree, this);

            // Remove leaf branches, except for those that are at the chunk boundary
            // It is possible that a two-way leaf branch existed, in which case the exterior vector is lost
            g.RemoveLeafBranches(v => chunkExterior.Contains(v.P));
            chunkExterior.RemoveWhere(e => !g.V.ContainsKey(e));

            return (g, chunkExterior);
        }

        /// <summary>
        /// Given a cumulative graph + boundary, merge new chunk into it.
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="existingBoundary"></param>
        /// <param name="adding"></param>
        /// <param name="addingBoundary"></param>
        protected abstract void StitchChunks(Graph<TV, TE> existing, HashSet<Vector3> existingBoundary,
            Graph<TV, TE> adding, HashSet<Vector3> addingBoundary);

        //public Func<AxialBounds, IEnumerable<AxialBounds>> ChunkGenerator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public abstract double GetRadius(TE e);

        /// <summary>
        /// Allows only intersections with suitably small vessels in the major vessel trees and
        /// enough overlap to be created.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="overlap"></param>
        /// <returns></returns>
        public abstract bool IsIntersectionPermitted(Segment segment, double overlap);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Segment Convert(TE e)
        {
            return Segment.MakeDummy(e.S.P, e.E.P, GetRadius(e));
        }

        //public IEnumerable<Segment> Generate(AxialBounds totalBounds)
        //{
        //    var G = new Graph<TV, TE>();
        //    foreach (var chunk in this.ChunkGenerator(totalBounds))
        //    {
        //        var g = GenerateChunk(chunk);
        //        StitchChunks(G, g);
        //    }
        //    return G.E.Values.Select(Convert);
        //}
    }

    /// <summary>
    /// Helper functions for continuous functional units
    /// </summary>
    public static class Continuous
    {
        public enum FaceViolation : int
        {
            LowerX = 0,
            LowerY = 1,
            LowerZ = 2,
            UpperX = 3,
            UpperY = 4,
            UpperZ = 5
        }

        public static bool AddViolations(AxialBounds bounds, Vector3 point, Vector3 other, ICollection<Vector3>[] faces)
        {
            var u = bounds.Upper;
            var l = bounds.Lower;

            var inside = true;
            if (other.x > u.x)
            {
                faces[(int)FaceViolation.UpperX].Add(point);
                inside = false;
            }
            else if (other.x < l.x)
            {
                faces[(int)FaceViolation.LowerX].Add(point);
                inside = false;
            }

            if (other.y > u.y)
            {
                faces[(int)FaceViolation.UpperY].Add(point);
                inside = false;
            }
            else if (other.y < l.y)
            {
                faces[(int)FaceViolation.LowerY].Add(point);
                inside = false;
            }

            if (other.z > u.z)
            {
                faces[(int)FaceViolation.UpperZ].Add(point);
                inside = false;
            }
            else if (other.z < l.z)
            {
                faces[(int)FaceViolation.LowerZ].Add(point);
                inside = false;
            }

            return inside;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <param name="graph"></param>
        /// <param name="vessels"></param>
        /// <param name="continuous"></param>
        public static void RemoveIllegalIntersections<TV, TE>(Graph<TV, TE> graph,
            IAxialBoundsQueryable<SegmentSurfaceTest> vessels, Continuous<TV, TE> continuous)
            where TV : Vertex<TV, TE>, new()
            where TE : Edge<TV, TE>, new()
        {
            bool predicate(TE edge)
            {
                var start = edge.S.P;
                var end = edge.E.P;
                var dir = end - start;
                var rad = continuous.GetRadius(edge);
                var queryBounds = new AxialBounds(start, end, rad);
                var rem = false;

                vessels.Query(queryBounds, test =>
                {
                    var overlap = test.Overlap(start, end, dir, rad, 1e-8);
                    if (overlap >= 0)
                    {
                        if (!continuous.IsIntersectionPermitted(test.Segment, overlap))
                        {
                            rem = true;
                        }
                    }
                });
                return rem;
            }

            var removing = graph.E.Values.Where(predicate).ToList();
            foreach (var r in removing)
            {
                graph.RemoveEdge(r);
            }
        }
    }
}
