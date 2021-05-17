using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Generators;
using Vascular.Geometry.Graphs;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Functionality.Capillary
{
    public class CapillaryDLA : CapillaryBase
    {
        public int Attempts { get; set; }

        public int Steps { get; set; } = 100;

        public Random Random { get; set; } = new();

        public double HitRadius { get; set; }

        public override (Graph<Vertex, Edge> graph, HashSet<Vector3> boundary) GenerateChunk(AxialBounds bounds)
        {
            var r = bounds.Range;
            var nv = r.Product / Math.Pow(this.Radius, 3);
            var g = new Graph((int)nv, (int)nv * 3);
            var br = new AxialBoundsRandom(bounds, this.Random);
            var sr = new SphericalRandom(this.Random);
            var segments = new List<Segment>();
            foreach (var n in this.Networks)
            {
                n.Query(bounds, seg => segments.Add(seg));
            }
            var segMap = new AxialBoundsHashTable<SegmentSurfaceTest>(
                segments.Select(s => new SegmentSurfaceTest(s)), this.HitRadius * 2);

            for (var i = 0; i < this.Attempts; ++i)
            {
                var x = br.NextVector3();
                // Test if we have hit anything yet
                for (var j = 0; j < this.Steps; ++j)
                {
                    Vector3 ndir = null;
                    Vector3 ap = null;
                    var qb = new AxialBounds(x, this.HitRadius);
                    segMap.Query(qb, segTest =>
                    {
                        var (d, n) = segTest.DistanceAndNormalToSurface(x);
                        if (d < segTest.Segment.Radius + this.HitRadius)
                        {
                            if (this.PermittedIntersection(segTest.Segment))
                            {
                                ap = x - (d + segTest.Segment.Radius) * n;
                            }
                            else
                            {
                                ndir = n;
                            }
                        }
                    });
                    if (ndir != null)
                    {
                        x += ndir * this.HitRadius;
                    }
                    else if (ap != null)
                    {
                        var edge = g.AddEdge(new Edge(g.AddVertex(x), g.AddVertex(ap)));
                        var seg = Convert(edge);
                        segMap.Add(new SegmentSurfaceTest(seg));
                        break;
                    }
                    else
                    {
                        x += sr.NextVector3() * this.HitRadius;
                    }

                    if (!bounds.Intersects(x))
                    {
                        break;
                    }
                }
            }
            return (g, null);
        }

        protected override void StitchChunks(Graph<Vertex, Edge> existing, HashSet<Vector3> existingBoundary,
            Graph<Vertex, Edge> adding, HashSet<Vector3> addingBoundary)
        {
            throw new NotImplementedException();
        }
    }
}
