using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Graphs;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Functionality
{
    /// <summary>
    /// Represents a continuous functional structure that attaches at many points to the networks.
    /// </summary>
    public abstract class Continuous<TV, TE>
        where TV : Vertex<TV, TE>, new()
        where TE : Edge<TV, TE>, new()
    {
        public abstract (Graph<TV, TE> graph, HashSet<Vector3> boundary) GenerateChunk(AxialBounds bounds);

        protected abstract void StitchChunks(Graph<TV, TE> existing, HashSet<Vector3> existingBoundary,
            Graph<TV, TE> adding, HashSet<Vector3> addingBoundary);

        public Func<AxialBounds, IEnumerable<AxialBounds>> ChunkGenerator { get; set; }

        public abstract double GetRadius(TE e);

        public abstract bool IsIntersectionPermitted(Segment segment, double overlap);

        public Segment Convert(TE e)
        {
            return new Segment()
            {
                Start = new Dummy() { Position = e.S.P },
                End = new Dummy() { Position = e.E.P },
                Radius = GetRadius(e)
            };
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

    public static class Continuous
    {
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
                    //var isect = new SegmentIntersection(continuous.Convert(edge), test.Segment, new Geometry.Generators.CubeGrayCode());
                    //if (isect.Intersecting)
                    //{
                    //    rem = true;
                    //}
                    var overlap = test.Overlap(start, end, dir, rad, 1e-8);
                    //if (isect.Intersecting && !overlap.Equals(isect.Overlap, 1.0e-6))
                    //{
                    //    throw new Exception();
                    //}
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
