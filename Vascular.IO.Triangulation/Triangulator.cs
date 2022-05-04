using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Lattices;
using Vascular.Geometry.Surfaces;
using Vascular.Geometry.Triangulation;
using Vascular.Structure;

namespace Vascular.IO.Triangulation
{
    /// <summary>
    /// 
    /// </summary>
    public record MeshChunks(int I, int J, int K);

    /// <summary>
    /// 
    /// </summary>
    public record ChunkPrepared(int Id, TimeSpan TimeElapsed, int Sample, int Build, int Segments, int Triangles);

    /// <summary>
    /// 
    /// </summary>
    public record ChunkSampled(int Id, TimeSpan TimeElapsed);

    /// <summary>
    /// 
    /// </summary>
    public record ChunkExtracted(int Id, TimeSpan TimeElapsed, int Triangles);

    /// <summary>
    /// 
    /// </summary>
    public record ChunkDecimating(int Id, object Data);

    /// <summary>
    /// 
    /// </summary>
    public record ChunkDecimated(int Id, TimeSpan TimeElapsed, int Triangles);
    
    /// <summary>
    /// 
    /// </summary>
    public record ChunkMerged(int Id, TimeSpan TimeElapsed);
    
    /// <summary>
    /// 
    /// </summary>
    public record MeshCreated(TimeSpan TimeElapsed, int Triangles);

    /// <summary>
    /// 
    /// </summary>
    public record MeshDecimated(TimeSpan TimeElapsed);

    /// <summary>
    /// Samples on a BCC lattice and generates faces using a Marching Tetrahedra approach, reducing each chunk as it goes.
    /// </summary>
    public class Triangulator
    {
        private BodyCentredCubicLattice lattice;
        private double stride;

        /// <summary>
        /// The sampling stride.
        /// </summary>
        public double Stride
        {
            get => stride;
            set
            {
                if (value > 0)
                {
                    stride = value;
                    lattice = new BodyCentredCubicLattice(value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int StridesPerChunk { get; set; } = 20;

        /// <summary>
        /// 
        /// </summary>
        public int MaxConcurrentChunks { get; set; } = 8;

        /// <summary>
        /// How many objects are expected to be hit in each chunk.
        /// </summary>
        public int PreloadCapacity { get; set; } = 1024;

        /// <summary>
        /// How many multiples of the stride do we set the hash table powers to be.
        /// </summary>
        public double PreloadStrideFactor { get; set; } = 4.0;

        /// <summary>
        /// The test rays for mesh intersections.
        /// </summary>
        public Vector3[] SurfaceTestDirections { get; set; } = Connectivity.CubeFaces;

        /// <summary>
        /// How far do we test each ray as a multiple of the largest bounding length.
        /// </summary>
        public double SurfaceTestRangeFactor { get; set; } = 2.0;

        /// <summary>
        /// The maximum number of allowed misses before deemed non-intersecting.
        /// </summary>
        public int SurfaceTestMaxMiss { get; set; } = 1;

        /// <summary>
        /// Extend points by this multiple of the stride when looking for features.
        /// </summary>
        public double PointBoundsExtensionFactor { get; set; } = 4.0;

        /// <summary>
        /// 
        /// </summary>
        public Action<Decimation> ConfigureChunkDecimation { get; set; } = d => { };

        /// <summary>
        /// 
        /// </summary>
        public Action<Decimation> ConfigureFinalDecimation { get; set; } = d => { };
        
        /// <summary>
        /// 
        /// </summary>
        public bool ReportChunkDecimation { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public bool ReportFinalDecimation { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public bool Decimate { get; set; } = true;

        private double vertexLower = 0.25;
        private double vertexUpper = 0.75;

        /// <summary>
        /// The fraction along each edge at which the vertices must be clamped to. Prevents collapsing to nodes.
        /// </summary>
        public double VertexFraction
        {
            get => vertexLower;
            set
            {
                if (value is > 0 and < 0.5)
                {
                    vertexLower = value;
                    vertexUpper = 1 - value;
                }
                else if (value is > 0.5 and < 1)
                {
                    vertexUpper = value;
                    vertexLower = 1 - value;
                }
            }
        }

        private readonly List<IAxialBoundsQueryable<Segment>> features = new();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public Triangulator Add(IAxialBoundsQueryable<Segment> feature)
        {
            features.Add(feature);
            return this;
        }

        private IAxialBoundsQueryable<TriangleSurfaceTest> boundary;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        public Triangulator Add(IAxialBoundsQueryable<TriangleSurfaceTest> surface)
        {
            boundary = surface;
            return this;
        }

        /// <summary>
        /// Finds the smallest feature radius, then sets stride to be <paramref name="fraction"/> times that.
        /// </summary>
        /// <param name="fraction"></param>
        /// <returns></returns>
        public Triangulator SetStrideFromFeatureRadii(double fraction)
        {
            var minFeatureRadius = features.Min(f => f.Where(s => s.Radius > 0.0).Min(s => s.Radius));
            this.Stride = minFeatureRadius * fraction;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Mesh> Export(IProgress<object> progress = null, CancellationToken cancellationToken = default)
        {
            var decimation = new Decimation(new Mesh());

            var totalBounds = features.GetTotalBounds()
                .Append(boundary?.GetAxialBounds() ?? new AxialBounds())
                .Extend(this.Stride * 2);
            var (iMin, jMin, kMin) = (totalBounds.Lower / this.Stride).Floor;
            var (iMax, jMax, kMax) = (totalBounds.Upper / this.Stride).Ceiling;
            var iNum = (int)Math.Ceiling((iMax - iMin + 1) / (double)this.StridesPerChunk);
            var jNum = (int)Math.Ceiling((jMax - jMin + 1) / (double)this.StridesPerChunk);
            var kNum = (int)Math.Ceiling((kMax - kMin + 1) / (double)this.StridesPerChunk);
            progress?.Report(new MeshChunks(iNum, jNum, kNum));

            IEnumerable<(int i, int j, int k, int n)> chunkLowerIndices()
            {
                var taskId = 0;
                for (var i = iMin; i <= iMax; i += this.StridesPerChunk)
                {
                    for (var j = jMin; j <= jMax; j += this.StridesPerChunk)
                    {
                        for (var k = kMin; k <= kMax; k += this.StridesPerChunk)
                        {
                            yield return (i, j, k, taskId);
                            taskId++;
                        }
                    }
                }
            }

            using var semaphore = new SemaphoreSlim(1);
            var stopwatch = Stopwatch.StartNew();
            await chunkLowerIndices().RunAsync(
                obj => GenerateChunk(
                    obj.i, obj.j, obj.k, iMax, jMax, kMax,
                    decimation, semaphore, obj.n, progress, cancellationToken),
                this.MaxConcurrentChunks, false, cancellationToken);
            var elapsed = stopwatch.Elapsed;

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new MeshCreated(elapsed, decimation.Mesh.T.Count));

            if (this.Decimate)
            {
                this.ConfigureFinalDecimation(decimation);
                var decimationProgress = this.ReportFinalDecimation ? progress : null;
                stopwatch.Restart();
                await decimation.Decimate(decimationProgress, cancellationToken);
                elapsed = stopwatch.Elapsed;

                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report(new MeshDecimated(elapsed));
            }

            return decimation.Mesh;
        }

        private async Task GenerateChunk(int iLo, int jLo, int kLo, int iMax, int jMax, int kMax,
            Decimation exportDecimation, SemaphoreSlim semaphore, int id, IProgress<object> progress,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var chunk = ExtractMesh(iLo, jLo, kLo, iMax, jMax, kMax,
                id, stopwatch, progress, cancellationToken);

            if (this.Decimate)
            {
                var chunkDecimation = new Decimation(chunk);
                this.ConfigureChunkDecimation(chunkDecimation);
                var chunkProgress = this.ReportChunkDecimation
                    ? new Progress<object>(data => progress?.Report(new ChunkDecimating(id, data)))
                    : null;
                stopwatch.Restart();
                await chunkDecimation.Decimate(chunkProgress, cancellationToken);
                var elapsed = stopwatch.Elapsed;
                progress?.Report(new ChunkDecimated(id, elapsed, chunk.T.Count));

                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    stopwatch.Restart();
                    exportDecimation.Merge(chunkDecimation);
                    elapsed = stopwatch.Elapsed;
                }
                finally
                {
                    semaphore.Release();
                }
                progress?.Report(new ChunkMerged(id, elapsed));
            }
            else
            {
                await semaphore.WaitAsync(cancellationToken);
                var elapsed = TimeSpan.Zero;
                try
                {
                    stopwatch.Restart();
                    exportDecimation.Mesh.Merge(chunk);
                    elapsed = stopwatch.Elapsed;
                }
                finally
                {
                    semaphore.Release();
                }
                progress?.Report(new ChunkMerged(id, elapsed));
            }
        }

        private Mesh ExtractMesh(int iLo, int jLo, int kLo, int iMax, int jMax, int kMax, 
            int id, Stopwatch stopwatch, IProgress<object> progress, CancellationToken cancellationToken)
        {
            // Not guaranteed to be spun up immediately
            cancellationToken.ThrowIfCancellationRequested();

            // Trim sample range down to required
            var iHi = Math.Min(iLo + this.StridesPerChunk, iMax + 1);
            var jHi = Math.Min(jLo + this.StridesPerChunk, jMax + 1);
            var kHi = Math.Min(kLo + this.StridesPerChunk, kMax + 1);

            var (points, sample) = GetPoints(iLo, iHi, jLo, jHi, kLo, kHi);
            var function = Sample(sample, id, points.Count, stopwatch, progress, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            stopwatch.Restart();
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
            var elapsed = stopwatch.Elapsed;
            progress?.Report(new ChunkExtracted(id, elapsed, chunk.T.Count));

            cancellationToken.ThrowIfCancellationRequested();

            return chunk;
        }

        private void TryGenerate(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, 
            double f0, double f1, double f2, double f3, Mesh chunk)
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

        private Vector3 GeneratePoint(Vector3 v0, Vector3 v1, double f0, double f1)
        {
            if (v0.CompareTo(v1) > 0)
            {
                var e = (f0 / (f0 - f1)).Clamp(vertexLower, vertexUpper);
                return e * v1 + (1 - e) * v0;
            }
            else
            {
                var e = (f1 / (f1 - f0)).Clamp(vertexLower, vertexUpper);
                return e * v0 + (1 - e) * v1;
            }
        }

        private void GenerateOne(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, 
            double f0, double f1, double f2, double f3, Mesh chunk)
        {
            var p1 = GeneratePoint(v0, v1, f0, f1);
            var p2 = GeneratePoint(v0, v2, f0, f2);
            var p3 = GeneratePoint(v0, v3, f0, f3);
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

        private void GenerateTwo(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, 
            double f0, double f1, double f2, double f3, Mesh chunk)
        {
            var p02 = GeneratePoint(v0, v2, f0, f2);
            var p03 = GeneratePoint(v0, v3, f0, f3);
            var p12 = GeneratePoint(v1, v2, f1, f2);
            var p13 = GeneratePoint(v1, v3, f1, f3);
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

        private static (List<Vector3> construction, HashSet<Vector3> sample) 
            GetPoints(int iLo, int iHi, int jLo, int jHi, int kLo, int kHi)
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
            for (var i = iLo; i < iHi; ++i)
            {
                for (var j = jLo; j < jHi; ++j)
                {
                    for (var k = kLo; k < kHi; ++k)
                    {
                        var z0 = new Vector3(i - k, j - k, 2 * k);
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

        private Dictionary<Vector3, double> Sample(HashSet<Vector3> points, int id, int build, Stopwatch stopwatch,
            IProgress<object> progress, CancellationToken cancellationToken)
        {
            Func<Vector3, AxialBounds, double, double> conversion;
            Func<Vector3, AxialBounds, double> initialize;

            var totalBounds = points.Select(p => lattice.ToSpace(p))
                .GetTotalBounds().Extend(this.Stride * this.PointBoundsExtensionFactor);

            var segments = new List<SegmentSurfaceTest>(this.PreloadCapacity);
            foreach (var f in features)
            {
                f.Query(totalBounds, s => segments.Add(new SegmentSurfaceTest(s)));
            }
            var lookupFeatures = new AxialBoundsHashTable<SegmentSurfaceTest>(segments, this.Stride * this.PreloadStrideFactor, 2);
            var triangleCount = 0;

            if (boundary == null)
            {
                initialize = (v, b) => double.PositiveInfinity;
                conversion = (v, b, d) =>
                {
                    lookupFeatures.Query(b, s => d = Math.Min(d, s.DistanceToSurface(v)));
                    return d;
                };
            }
            else
            {
                var testDirections = this.SurfaceTestDirections
                    .Select(d => d * boundary.GetAxialBounds().Range.Max * this.SurfaceTestRangeFactor)
                    .ToArray();
                var minHits = testDirections.Length - this.SurfaceTestMaxMiss;

                var triangles = new List<TriangleSurfaceTest>(this.PreloadCapacity);
                boundary.Query(totalBounds, t => triangles.Add(t));
                var boundaryFeatures = new AxialBoundsHashTable<TriangleSurfaceTest>(triangles, this.Stride * this.PreloadStrideFactor, 2);
                triangleCount = triangles.Count;

                initialize = (v, b) =>
                {
                    var sign = boundary.IsPointInside(v, testDirections, minHits) ? -1.0 : 1.0;
                    var dist2 = double.PositiveInfinity;
                    boundaryFeatures.Query(b, t => dist2 = Math.Min(dist2, t.DistanceSquared(v)));
                    return sign * Math.Sqrt(dist2);
                };
                conversion = (v, b, d) =>
                {
                    lookupFeatures.Query(b, s => d = Math.Max(d, -s.DistanceToSurface(v)));
                    return d;
                };
            }

            var elapsed = stopwatch.Elapsed;
            progress?.Report(new ChunkPrepared(id, elapsed, points.Count, build, segments.Count, triangleCount));

            cancellationToken.ThrowIfCancellationRequested();

            stopwatch.Restart();
            var function = points.ToDictionary(p => lattice.ToSpace(p), p =>
            {
                var v = lattice.ToSpace(p);
                var b = new AxialBounds(v).Extend(this.Stride * this.PointBoundsExtensionFactor);
                var d = initialize(v, b);
                return conversion(v, b, d);
            });
            elapsed = stopwatch.Elapsed;
            progress?.Report(new ChunkSampled(id, elapsed));

            return function;
        }
    }
}
