using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Acceleration;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Lattices;
using Vascular.Geometry.Triangulation;
using Vascular.Structure;

namespace Vascular.IO.Triangulation
{
    public class MarchingTetrahedra
    {
        private BodyCentredCubicLattice lattice;
        private double stride;
        public double Stride
        {
            get
            {
                return stride;
            }
            set
            {
                if (value > 0)
                {
                    stride = value;
                    lattice = new BodyCentredCubicLattice(value);
                }
            }
        }
        public int StridesPerDivision { get; set; } = 20;
        public int MaxThreads { get; set; } = 8;

        public bool RoundVectors { get; set; } = true;
        public int DecimalPlaces { get; set; } = 10;

        public bool PreloadCandidates { get; set; } = true;
        public int PreloadCapacity { get; set; } = 1024;
        public double PreloadStrideFactor { get; set; } = 4.0;

        private double vertexLower = 0.25;
        private double vertexUpper = 0.75;
        public double VertexFraction
        {
            get
            {
                return vertexLower;
            }
            set
            {
                if (value > 0 && value < 0.5)
                {
                    vertexLower = value;
                    vertexUpper = 1.0 - value;
                }
            }
        }

        //public Action<Mesh> ChunkCompleteAction { get; set; }
        //public Action<Mesh> ExportCompleteAction { get; set; }

        //public delegate void ExportCompletionAction();
        //public ExportCompletionAction ExportComplete { get; set; }
        //public TriangleCompletionAction TriangleComplete { get; set; }
        //public event ChunkCompletionAction OnChunkComplete;
        //public event ExportCompletionAction OnExportComplete;

        //private Mesh chunk;
        //public MarchingTetrahedonExporter Add(Mesh chunk)
        //{
        //    this.chunk = chunk;
        //    return this;
        //}

        //private readonly List<Network> networks = new List<Network>();
        //public MarchingTetrahedonExporter Add(Network network)
        //{
        //    networks.Add(network);
        //    return this;
        //}

        private readonly List<IAxialBoundsQueryable<Segment>> features = new List<IAxialBoundsQueryable<Segment>>();
        public MarchingTetrahedra Add(IAxialBoundsQueryable<Segment> feature)
        {
            features.Add(feature);
            return this;
        }

        public MarchingTetrahedra SetStrideFromFeatureRadii(double fraction)
        {
            //var minVesselRadius = networks.Min(n => n.Segments.Where(s => s.Radius > 0.0).Min(s => s.Radius));
            var minFeatureRadius = features.Min(f => f.Where(s => s.Radius > 0.0).Min(s => s.Radius));
            this.Stride = minFeatureRadius * fraction;
            //this.Stride = Math.Min(minVesselRadius, minFeatureRadius) * fraction;
            return this;
        }

        public Mesh Export()
        {
            var export = new Mesh();
            var totalBounds = features.Select(f => f is IAxialBoundable ab ? ab.GetAxialBounds() : f.GetTotalBounds())
                .GetTotalBounds().Extend(this.Stride * 2);
            var (iLo, jLo, kLo) = (totalBounds.Lower / this.Stride).Floor;
            var (iHi, jHi, kHi) = (totalBounds.Upper / this.Stride).Ceiling;
            var tasks = new HashSet<Task>(this.MaxThreads);
            IEnumerable<(int i, int j, int k)> chunkLowerIndices()
            {
                for (var i = iLo; i <= iHi; i += this.StridesPerDivision)
                {
                    for (var j = jLo; j <= jHi; j += this.StridesPerDivision)
                    {
                        for (var k = kLo; k <= kHi; k += this.StridesPerDivision)
                        {
                            yield return (i, j, k);
                        }
                    }
                }
            }
            foreach (var (i, j, k) in chunkLowerIndices())
            {
                if (tasks.Count == this.MaxThreads)
                {
                    var wait = Task.WhenAny(tasks);
                    wait.Wait();
                    tasks.Remove(wait.Result);
                }
                tasks.Add(Task.Run(() => GenerateChunk(i, j, k, export)));
            }
            //this.ExportCompleteAction(export);
            return export;
        }

        private void GenerateChunk(int iLo, int jLo, int kLo, Mesh export)
        {
            var (points, sample) = GetPoints(iLo, iLo + this.StridesPerDivision, jLo, jLo + this.StridesPerDivision, kLo, kLo + this.StridesPerDivision);
            var function = Sample(sample, lattice);
            var chunk = new Mesh();
            foreach (var p in points)
            {
                var v000 = lattice.ToSpace(p);
                var v001 = lattice.ToSpace(p + ConstructionPattern[0]);
                var v010 = lattice.ToSpace(p + ConstructionPattern[1]);
                var v100 = lattice.ToSpace(p + ConstructionPattern[2]);
                var v011 = lattice.ToSpace(p + ConstructionPattern[3]);
                var v110 = lattice.ToSpace(p + ConstructionPattern[4]);
                var v101 = lattice.ToSpace(p + ConstructionPattern[5]);
                var v111 = lattice.ToSpace(p + ConstructionPattern[6]);
                var f000 = function[v000];
                var f001 = function[v001];
                var f010 = function[v010];
                var f100 = function[v100];
                var f011 = function[v011];
                var f110 = function[v110];
                var f101 = function[v101];
                var f111 = function[v111];
                TryGenerate(v000, v001, v010, v100, f000, f001, f010, f100, chunk);
                TryGenerate(v111, v110, v101, v011, f111, f110, f101, f011, chunk);
                TryGenerate(v100, v011, v001, v010, f100, f011, f001, f010, chunk);
                TryGenerate(v100, v011, v001, v101, f100, f011, f001, f101, chunk);
                TryGenerate(v100, v011, v110, v010, f100, f011, f110, f010, chunk);
                TryGenerate(v100, v011, v110, v101, f100, f011, f110, f101, chunk);
            }
            //this.ChunkCompleteAction(chunk);
            lock (export)
            {
                export.Merge(chunk);
            }
        }

        private void TryGenerate(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, double f0, double f1, double f2, double f3, Mesh chunk)
        {
            var b0 = f0 <= 0;
            var b1 = f1 <= 0;
            var b2 = f2 <= 0;
            var b3 = f3 <= 0;
            // There are now 16 cases, symmetry will reduce to 8
            if (b0)
            {
                if (b1)
                {
                    if (b2)
                    {
                        if (b3)
                        {
                            // All in
                            return;
                        }
                        else
                        {
                            // 0,1,2 in, 3 out
                            GenerateOne(v3, v0, v1, v2, f3, f0, f1, f2, chunk);
                        }
                    }
                    else
                    {
                        if (b3)
                        {
                            // 0,1,3 in, 2 out
                            GenerateOne(v2, v0, v1, v3, f2, f0, f1, f3, chunk);
                        }
                        else
                        {
                            // 0,1 in, 2,3 out
                            GenerateTwo(v0, v1, v2, v3, f0, f1, f2, f3, chunk);
                        }
                    }
                }
                else
                {
                    if (b2)
                    {
                        if (b3)
                        {
                            // 0,2,3 in, 1 out
                            GenerateOne(v1, v0, v2, v3, f1, f0, f2, f3, chunk);
                        }
                        else
                        {
                            // 0,2 in, 1,3 out
                            GenerateTwo(v0, v2, v1, v3, f0, f2, f1, f3, chunk);
                        }
                    }
                    else
                    {
                        if (b3)
                        {
                            // 0,3 in, 1,2 out
                            GenerateTwo(v0, v3, v1, v2, f0, f3, f1, f2, chunk);
                        }
                        else
                        {
                            // 0 in, 1,2,3 out
                            GenerateOne(v0, v1, v2, v3, f0, f1, f2, f3, chunk);
                        }
                    }
                }
            }
            else
            {
                if (b1)
                {
                    if (b2)
                    {
                        if (b3)
                        {
                            // 1,2,3 in, 0 out
                            GenerateOne(v0, v1, v2, v3, f0, f1, f2, f3, chunk);
                        }
                        else
                        {
                            // 1,2 in, 0,3 out
                            GenerateTwo(v0, v3, v1, v2, f0, f3, f1, f2, chunk);
                        }
                    }
                    else
                    {
                        if (b3)
                        {
                            // 1,3 in, 0,2 out
                            GenerateTwo(v0, v2, v1, v3, f0, f2, f1, f3, chunk);
                        }
                        else
                        {
                            // 1 in, 0,2,3 out
                            GenerateOne(v1, v0, v2, v3, f1, f0, f2, f3, chunk);
                        }
                    }
                }
                else
                {
                    if (b2)
                    {
                        if (b3)
                        {
                            // 2,3, in, 0,1 out
                            GenerateTwo(v0, v1, v2, v3, f0, f1, f2, f3, chunk);
                        }
                        else
                        {
                            // 2 in, 0,1,3 out
                            GenerateOne(v2, v0, v1, v3, f2, f0, f1, f3, chunk);
                        }
                    }
                    else
                    {
                        if (b3)
                        {
                            // 3 in, 0,1,2 out
                            GenerateOne(v3, v0, v1, v2, f3, f0, f1, f2, chunk);
                        }
                        else
                        {
                            // All out
                            return;
                        }
                    }
                }
            }
        }

        private static readonly Vector3[] ConstructionPattern = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 1)
        };

        private void GenerateOne(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, double f0, double f1, double f2, double f3, Mesh chunk)
        {
            // v0 is the odd one out
            var e1 = (f0 / (f0 - f1)).Clamp(vertexLower, vertexUpper);
            var e2 = (f0 / (f0 - f2)).Clamp(vertexLower, vertexUpper);
            var e3 = (f0 / (f0 - f3)).Clamp(vertexLower, vertexUpper);
            var p1 = e1 * v1 + (1 - e1) * v0;
            var p2 = e2 * v2 + (1 - e2) * v0;
            var p3 = e3 * v3 + (1 - e3) * v0;
            // Rounding vectors can ensure that we don't accidentally duplicate due to machine precision errors
            if (this.RoundVectors)
            {
                p1 = p1.Round(this.DecimalPlaces);
                p2 = p2.Round(this.DecimalPlaces);
                p3 = p3.Round(this.DecimalPlaces);
            }
            // Make sure that we are wound correctly
            var n = (p2 - p1) ^ (p3 - p1);
            if (f0 <= 0)
            {
                // Ensure that the triangle points away from v0
                if (n * (p1 - v0) > 0)
                {
                    chunk.AddTriangle(p1, p2, p3);
                }
                else
                {
                    chunk.AddTriangle(p1, p3, p2);
                }
            }
            else
            {
                // The triangle normal must point towards v0
                if (n * (v0 - p1) > 0)
                {
                    chunk.AddTriangle(p1, p2, p3);
                }
                else
                {
                    chunk.AddTriangle(p1, p3, p2);
                }
            }
        }

        private void GenerateTwo(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, double f0, double f1, double f2, double f3, Mesh chunk)
        {
            // v0,v1 - v2,v3 pairs
            var e02 = (f0 / (f0 - f2)).Clamp(vertexLower, vertexUpper);
            var e03 = (f0 / (f0 - f3)).Clamp(vertexLower, vertexUpper);
            var e12 = (f1 / (f1 - f2)).Clamp(vertexLower, vertexUpper);
            var e13 = (f1 / (f1 - f3)).Clamp(vertexLower, vertexUpper);
            var p02 = e02 * v2 + (1 - e02) * v0;
            var p03 = e03 * v3 + (1 - e03) * v0;
            var p12 = e12 * v2 + (1 - e12) * v1;
            var p13 = e13 * v3 + (1 - e13) * v1;
            if (this.RoundVectors)
            {
                p02 = p02.Round(this.DecimalPlaces);
                p03 = p03.Round(this.DecimalPlaces);
                p12 = p12.Round(this.DecimalPlaces);
                p13 = p13.Round(this.DecimalPlaces);
            }
            var n = (p12 - p02) ^ (p03 - p02);
            // Generate triangles: 02 12 03 - 03 12 13, ensuring that winding correct
            if (f0 <= 0)
            {
                // Patch must point away from v0
                if (n * (p02 - v0) > 0)
                {
                    chunk.AddTriangle(p02, p12, p03);
                    chunk.AddTriangle(p03, p12, p13);
                }
                else
                {
                    chunk.AddTriangle(p02, p03, p12);
                    chunk.AddTriangle(p03, p13, p12);
                }
            }
            else
            {
                // Patch must point towards v0
                if (n * (v0 - p02) > 0)
                {
                    chunk.AddTriangle(p02, p12, p03);
                    chunk.AddTriangle(p03, p12, p13);
                }
                else
                {
                    chunk.AddTriangle(p02, p03, p12);
                    chunk.AddTriangle(p03, p13, p12);
                }
            }
        }

        private static (List<Vector3> construction, HashSet<Vector3> sample) GetPoints(int iLo, int iHi, int jLo, int jHi, int kLo, int kHi)
        {
            // Sublattice with integral transform (1,0,0) (0,1,0) (0,0,1) <-> (1,0,0) (0,1,0) (-1,-1,2)
            // gets us cubic sublattice of index 2. Work in this to fill bounds based on stride
            // Points exclusive of end
            var iR = iHi - iLo;
            var jR = jHi - jLo;
            var kR = kHi - kLo;
            var total = iR * jR * kR * 2;
            var construction = new List<Vector3>(total);
            var extra = (iR * jR + jR * kR + kR * iR + 1) * 2;
            var sample = new HashSet<Vector3>(total + extra);
            for (var i = iLo; i <= iHi; ++i)
            {
                for (var j = jLo; j <= jHi; ++j)
                {
                    for (var k = kLo; k <= kHi; ++k)
                    {
                        var z0 = new Vector3(i, j, 2 * k - i - j);
                        var z1 = z0 + new Vector3(0, 0, 1);
                        construction.Add(z0);
                        construction.Add(z1);
                        AddSamplePoints(sample, z0);
                        AddSamplePoints(sample, z1);
                    }
                }
            }
            return (construction, sample);
        }

        private static void AddSamplePoints(HashSet<Vector3> samples, Vector3 sample)
        {
            samples.Add(sample);
            foreach (var p in ConstructionPattern)
            {
                samples.Add(sample + p);
            }
        }

        private double DefaultSampler(Vector3 v, AxialBounds b, double d)
        {
            foreach (var f in features)
            {
                f.Query(b, s => d = Math.Min(d, s.DistanceToSurface(v)));
            }
            return d;
        }

        private Dictionary<Vector3, double> Sample(HashSet<Vector3> points, Lattice lattice)
        {
            Func<Vector3, AxialBounds, double, double> conversion = DefaultSampler;

            if (this.PreloadCandidates)
            {
                var segments = new List<SegmentSurfaceTest>(this.PreloadCapacity);
                var totalBounds = points.GetTotalBounds().Extend(this.Stride * 2);
                foreach (var f in features)
                {
                    f.Query(totalBounds, s => segments.Add(new SegmentSurfaceTest(s)));
                }
                var lookup = new AxialBoundsHashTable<SegmentSurfaceTest>(segments, this.Stride * this.PreloadStrideFactor, 2);
                conversion = (v, b, d) =>
                {
                    lookup.Query(b, s => d = Math.Min(d, s.DistanceToSurface(v)));
                    return d;
                };
            }

            return points.ToDictionary(p => p, p =>
            {
                var v = lattice.ToSpace(p);
                var b = new AxialBounds(v).Extend(this.Stride * 2);
                var d = this.Stride * 2;
                return conversion(v, b, d);
            });
        }
    }
}
