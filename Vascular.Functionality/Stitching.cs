using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Graphs;
using Vascular.Structure;

namespace Vascular.Functionality
{
    /// <summary>
    ///
    /// </summary>
    public class Stitching<TV, TE>
        where TV : Vertex<TV, TE>, new()
        where TE : Edge<TV, TE>, new()
    {
        private record Chunk(AxialBounds Bounds, Graph<TV, TE> Graph, HashSet<Vector3> Boundary) : IAxialBoundable
        {
            public AxialBounds GetAxialBounds()
            {
                return this.Bounds;
            }
        }

        private readonly AxialBoundsHashTable<Chunk> chunks;
        private readonly Continuous<TV, TE> continuous;
        private readonly IAxialBoundsQueryable<Segment>[] major;

        /// <summary>
        ///
        /// </summary>
        /// <param name="continuous"></param>
        /// <param name="major"></param>
        /// <param name="stride"></param>
        /// <param name="factor"></param>
        public Stitching(Continuous<TV, TE> continuous, IAxialBoundsQueryable<Segment>[] major,
            double stride = 1, double factor = 2)
        {
            this.continuous = continuous;
            this.major = major;
            chunks = new(Array.Empty<Chunk>(), stride, factor);
        }

        private readonly Graph<TV, TE> graph = new();
        private readonly SemaphoreSlim graphSemaphore = new(1);

        /// <summary>
        ///
        /// </summary>
        public int MaxConcurrency { get; set; } = 1;

        /// <summary>
        ///
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public async Task Add(IEnumerable<AxialBounds> bounds)
        {
            await bounds.RunAsync(b => Add(b), this.MaxConcurrency);
        }

        private async Task Add(AxialBounds bounds)
        {
            var chunk = Create(bounds);

            await graphSemaphore.WaitAsync();
            try
            {
                Stitch(chunk);
                chunks.Add(chunk);
            }
            finally
            {
                graphSemaphore.Release();
            }
        }

        private Chunk Create(AxialBounds bounds)
        {
            var (g, dG) = continuous.GenerateChunk(bounds, major);
            return new Chunk(bounds, g, dG);
        }

        /// <summary>
        ///
        /// </summary>
        public int BoundarySizeMultiplier { get; set; } = 26;

        private void Stitch(Chunk chunk)
        {
            // Performed before new chunk added to table, so never duplicates.
            var dG = new HashSet<Vector3>(chunk.Boundary.Count * this.BoundarySizeMultiplier);
            chunks.Query(chunk.Bounds, c => dG.UnionWith(c.Boundary));
            continuous.StitchChunk(graph, dG, chunk.Graph, chunk.Boundary, major);
        }

        /// <summary>
        /// Scans the whole graph, gets all edges which intersect the output bounds and
        /// removes all edges which are completely contained in this bounds.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public Graph<TV, TE> Take(AxialBounds bounds)
        {
            Trim(bounds);

            var output = new Graph<TV, TE>();
            var removing = new List<TE>();
            foreach (var e in graph.E.Values)
            {
                var ab = new AxialBounds(e.S.P, e.E.P, continuous.GetRadius(e));
                if (bounds.Intersects(ab))
                {
                    output.AddEdge(e.S.P, e.E.P);

                    if (bounds.Contains(ab))
                    {
                        removing.Add(e);
                    }
                }
            }

            foreach (var e in removing)
            {
                graph.RemoveEdge(e);
            }
            return output;
        }

        private void Trim(AxialBounds bounds)
        {
            bool validTrim(TV v)
            {
                // Leaf branches which end in the export region cannot be fixed.
                // Those which start outside but end inside are also removed, it is possible
                // that they could be fixed (e.g. through another chunk entirely) but these
                // should be so long that it is not justified to save them.
                if (bounds.Intersects(v.P))
                {
                    return true;
                }
                var (V, _) = v.WalkBranch<TV, TE>();
                return bounds.Intersects(V.P);
            }

            graph.RemoveLeafBranches(v => !validTrim(v));
        }
    }
}
