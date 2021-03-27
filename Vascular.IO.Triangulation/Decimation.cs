using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Surfaces;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.Triangulation
{
    public record DecimationBegin(int BoundaryVertices, int Vertices);
    public record DecimationStep(int Triangles, int Edges, int Remesh, int Recost, int Valid, Summary Error);
    public record DecimationPass(int Triangles, int Edges, Summary Error);

    public enum DecimationMode
    {
        Full,
        Local,
        Greedy
    }

    public class Decimation
    {
        public double MaxErrorSquared { get; set; } = 0.0;
        public double NormalToleranceSquare { get; set; } = 1.0e-12;
        public Mesh Mesh { get; }

        public bool ReportErrors { get; set; }
        public IEnumerable<int> SummaryMoments { get; set; }
        public IEnumerable<double> SummaryOrders { get; set; }

        public Func<int, int> EdgesPerChunk { get; set; } = i => 1024;
        public int MaxConcurrentChunks { get; set; } = 1;

        public bool DropErrors { get; set; } = false;

        public Decimation(Mesh mesh)
        {
            this.Mesh = mesh ?? new Mesh();
            ClearErrors();
            //RemeshAll();
        }

        public void ClearErrors()
        {
            originalPoints = this.Mesh.T.ToDictionary(t => t, t => new OriginalPoints(Array.Empty<Vector3>(), 0.0));
        }

        public void RemeshAll()
        {
            remeshing = new HashSet<(Vertex kept, Vertex lost)>(this.Mesh.E.Count * 2);
            foreach (var e in this.Mesh.E.Values)
            {
                remeshing.Add((e.S, e.E));
                remeshing.Add((e.E, e.S));
            }
            recosting = new HashSet<(Vertex kept, Vertex lost)>(this.Mesh.E.Count * 2);
            collapses = new Dictionary<(Vertex kept, Vertex lost), Collapse>(this.Mesh.E.Count * 2);
        }

        private bool boundaryValid = false;

        private void SetBoundary()
        {
            boundaryVertices = this.Mesh.V.Values.Where(v => !v.IsInterior).ToHashSet();
            if (this.ExtendBoundary)
            {
                foreach (var b in boundaryVertices.ToArray())
                {
                    foreach (var v in b.UnorderedFan)
                    {
                        boundaryVertices.Add(v);
                    }
                }
            }
            boundaryValid = true;
        }

        private void Begin(IProgress<object> progress)
        {
            if (!boundaryValid)
            {
                SetBoundary();
            }
            progress?.Report(new DecimationBegin(boundaryVertices.Count, this.Mesh.V.Count));
            RemeshAll();
        }

        public bool ExtendBoundary { get; set; } = true;

        public void Merge(Decimation other)
        {
            boundaryValid = false;

            originalPoints.EnsureCapacity(originalPoints.Count + other.originalPoints.Count);
            foreach (var t in other.Mesh.T)
            {
                var T = this.Mesh.AddTriangle(t.A.P, t.B.P, t.C.P);
                originalPoints[T] = other.originalPoints[t];
            }

            //collapses.EnsureCapacity(collapses.Count + other.collapses.Count);
            //foreach (var c in other.collapses.Values)
            //{
            //    var C = Translate(c);
            //    collapses[(C.Kept, C.Lost)] = C;
            //}

            //recosting.EnsureCapacity(recosting.Count + other.recosting.Count);
            //foreach (var (k, l) in other.recosting)
            //{
            //    recosting.Add((Translate(k), Translate(l)));
            //}
            //remeshing.EnsureCapacity(remeshing.Count + other.remeshing.Count);
            //foreach (var (k, l) in other.remeshing)
            //{
            //    remeshing.Add((Translate(k), Translate(l)));
            //}
        }

        private Vertex Translate(Vertex other)
        {
            return this.Mesh.V[other.P];
        }

        private Collapse Translate(Collapse other)
        {
            var T = other.Remesh.Triangles;
            var triangles = new VertexTriple[T.Length];
            for (var i = 0; i < T.Length; ++i)
            {
                triangles[i] = T[i] with
                {
                    A = Translate(T[i].A),
                    B = Translate(T[i].B),
                    C = Translate(T[i].C)
                };
            }
            var remesh = other.Remesh with { Triangles = triangles };
            var fan = new List<Vertex>(other.Fan.Count);
            foreach (var v in fan)
            {
                fan.Add(Translate(v));
            }
            return other with
            {
                Kept = Translate(other.Kept),
                Lost = Translate(other.Lost),
                Remesh = remesh,
                Fan = fan
            };
        }

        private record OriginalPoints(Vector3[] Points, double Error);
        private record VertexTriple(Vertex A, Vertex B, Vertex C, TriangleSurfaceTest Surface);
        private record Remesh(VertexTriple[] Triangles, OriginalPoints[] Points,
            double NewError, double OldError, double NewShapeCost, double OldShapeCost);

        private record Collapse(Vertex Kept, Vertex Lost, Remesh Remesh, List<Vertex> Fan, double Cost);

        private Dictionary<Triangle, OriginalPoints> originalPoints;
        private Dictionary<(Vertex kept, Vertex lost), Collapse> collapses;
        private HashSet<(Vertex kept, Vertex lost)> remeshing;
        private HashSet<(Vertex kept, Vertex lost)> recosting;
        private HashSet<Vertex> boundaryVertices;

        public DecimationMode Mode { get; set; } = DecimationMode.Local;

        public async Task Decimate(IProgress<object> progress = null, CancellationToken cancellationToken = default)
        {
            if (this.MaxErrorSquared <= 0.0)
            {
                return;
            }
            Begin(progress);
            switch (this.Mode)
            {
                case DecimationMode.Full:
                    await DecimateFull(progress, cancellationToken);
                    break;
                case DecimationMode.Local:
                    await DecimateLocal(TryCollapseLocal, progress, cancellationToken);
                    break;
                case DecimationMode.Greedy:
                    await DecimateLocal(TryCollapseGreedy, progress, cancellationToken);
                    break;
            }
            //FilterCache();
        }

        private Task<Summary> GetSummary()
        {
            if (this.ReportErrors)
            {
                var errors = originalPoints.Values.Select(op => op.Error).ToArray();
                return Task.Run(() => new Summary(errors, this.SummaryMoments, this.SummaryOrders));
            }
            return Task.FromResult<Summary>(null);
        }

        private async Task DecimateFull(IProgress<object> progress = null, CancellationToken cancellationToken = default)
        {
            //if (!boundaryValid)
            //{
            //    SetBoundary();
            //}
            //progress?.Report(new DecimationBegin(boundaryVertices.Count, this.Mesh.V.Count));
            while (remeshing.Count != 0 || recosting.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remesh = remeshing.Count;
                var recost = recosting.Count;
                var summary = GetSummary();

                var valid = await GetCollapses(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new DecimationStep(this.Mesh.T.Count, this.Mesh.E.Count, remesh, recost, valid.Count(), await summary));
                foreach (var collapse in valid)
                {
                    Execute(collapse);
                }
                FinishIteration();
            }
        }

        private async Task<IOrderedEnumerable<Collapse>> GetCollapses(CancellationToken cancellationToken)
        {
            var validCollapses = new List<Collapse>(this.Mesh.E.Count * 2);
            var recost = GetRecosting(validCollapses, cancellationToken);
            var remesh = GetRemeshing(validCollapses, cancellationToken);
            await recost;
            recosting.Clear();
            await remesh;
            remeshing.Clear();
            return validCollapses.OrderBy(c => c.Cost);
        }

        private readonly SemaphoreSlim collapseSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim validSemaphore = new SemaphoreSlim(1);

        private static IEnumerable<(int start, int end)> EnumerateChunks(int total, int stride)
        {
            for (var start = 0; start < total; start += stride)
            {
                yield return (start, Math.Min(start + stride, total));
            }
        }

        private Task GetRemeshing(List<Collapse> validCollapses, CancellationToken cancellationToken)
        {
            var workingArray = remeshing.ToArray();
            return EnumerateChunks(remeshing.Count, this.EdgesPerChunk(remeshing.Count)).RunAsync(
                async range =>
                {
                    var (begin, end) = range;
                    var valid = new List<Collapse>(end - begin);
                    var invalid = new List<Collapse>(end - begin);
                    var removing = new List<(Vertex kept, Vertex lost)>(end - begin);
                    for (var j = begin; j < end; ++j)
                    {
                        var (kept, lost) = workingArray[j];
                        if (TryRemesh(kept.EdgeTo(lost), kept, lost, out var remesh, out var fan))
                        {
                            if (TryCollapse(kept, lost, remesh, fan, out var collapse))
                            {
                                valid.Add(collapse);
                            }
                            else
                            {
                                invalid.Add(new Collapse(kept, lost, remesh, fan, double.PositiveInfinity));
                            }
                        }
                        else
                        {
                            removing.Add((kept, lost));
                        }
                    }

                    // Update the collapses with new valid ones
                    await collapseSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        foreach (var collapse in invalid)
                        {
                            collapses[(collapse.Kept, collapse.Lost)] = collapse;
                        }
                        foreach (var collapse in valid)
                        {
                            collapses[(collapse.Kept, collapse.Lost)] = collapse;
                        }
                        foreach (var key in removing)
                        {
                            collapses.Remove(key);
                        }
                    }
                    finally
                    {
                        collapseSemaphore.Release();
                    }

                    await validSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        validCollapses.AddRange(valid);
                    }
                    finally
                    {
                        validSemaphore.Release();
                    }
                }, this.MaxConcurrentChunks, false, cancellationToken);
        }

        private Task GetRecosting(List<Collapse> validCollapses, CancellationToken cancellationToken)
        {
            var workingArray = recosting.ToArray();
            return EnumerateChunks(recosting.Count, this.EdgesPerChunk(recosting.Count)).RunAsync(
                async range =>
                {
                    var (begin, end) = range;
                    var valid = new List<Collapse>(end - begin);
                    var removing = new List<(Vertex kept, Vertex lost)>(end - begin);
                    for (var j = begin; j < end; ++j)
                    {
                        var V = workingArray[j];
                        // Test for not being in remeshing as a remesh may have occurred already and readded
                        if (!remeshing.Contains(V) && collapses.TryGetValue(V, out var old))
                        {
                            // An edge case that was found in testing - if the model had holes smaller than the
                            // tolerance (e.g. sampled too coarse) then the neighbouring edges could collapse
                            // into a 3-edge hole which isn't caught by the remesh invalidation checks
                            if (V.kept.EdgeTo(V.lost) is not Edge edge || !edge.CanCollapse)
                            {
                                removing.Add(V);
                            }
                            else if (TryCollapse(old.Kept, old.Lost, old.Remesh, old.Fan, out var updated))
                            {
                                valid.Add(updated);
                            }
                        }
                    }

                    await collapseSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        foreach (var key in removing)
                        {
                            collapses.Remove(key);
                        }
                    }
                    finally
                    {
                        collapseSemaphore.Release();
                    }

                    await validSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        validCollapses.AddRange(valid);
                    }
                    finally
                    {
                        validSemaphore.Release();
                    }
                }, this.MaxConcurrentChunks, false, cancellationToken);
        }

        private void FinishIteration()
        {
            if (this.DropErrors)
            {
                ClearErrors();
            }
        }

        private void Execute(Collapse collapse)
        {
            if (CheckValid(collapse))
            {
                RemoveLostEdges(collapse);
                ModifyMesh(collapse);
                MarkInvalidated(collapse);
            }
        }

        private bool CheckValid(Collapse collapse)
        {
            var V = (collapse.Kept, collapse.Lost);
            return !remeshing.Contains(V)
                && !recosting.Contains(V)
                && collapses.ContainsKey(V);
        }

        private void ModifyMesh(Collapse collapse)
        {
            var removedTriangles = collapse.Lost.T.ToArray();
            foreach (var t in removedTriangles)
            {
                originalPoints.Remove(t);
                this.Mesh.RemoveTriangle(t);
            }
            for (var i = 0; i < collapse.Remesh.Triangles.Length; ++i)
            {
                var t = collapse.Remesh.Triangles[i];
                var T = this.Mesh.AddTriangle(t.A.P, t.B.P, t.C.P);
                originalPoints[T] = collapse.Remesh.Points[i];
            }
        }

        private void RemoveLostEdges(Collapse collapse)
        {
            // Called before modifying, so lost vertex still exists
            // Invalid remesh now for every edge containing lost vertex
            // No chance of recalculating these though
            foreach (var edge in collapse.Lost.E)
            {
                collapses.Remove((edge.S, edge.E));
                collapses.Remove((edge.E, edge.S));
            }
        }

        private void MarkInvalidated(Collapse collapse)
        {
            // Called after modifying, so lost vertex is gone - no need to check before readding
            // Invalid remesh for every collapse that loses a vertex in the fan
            // But collapses into the fan don't need remeshing, just recosting
            // Only recost if edge belongs to triangle attached to fan.
            foreach (var fanVertex in collapse.Fan)
            {
                foreach (var edge in fanVertex.E)
                {
                    var other = edge.Other(fanVertex);
                    remeshing.Add((other, fanVertex));
                    // Now test if any triangle on this edge borders a fan edge
                    foreach (var tri in edge.T)
                    {
                        if (collapse.Fan.Contains(tri.Opposite(edge)))
                        {
                            recosting.Add((fanVertex, other));
                            break;
                        }
                    }
                }
            }
        }

        private bool TryCollapse(Vertex kept, Vertex lost, Remesh remesh, List<Vertex> fan, out Collapse collapse)
        {
            collapse = null;
            var oldAngleCost = 0.0;
            var newAngleCost = 0.0;
            if (this.DihedralAngleCost != null)
            {
                if (!RemeshDihedralSum(lost, fan, remesh.Triangles, out newAngleCost))
                {
                    return false;
                }
                oldAngleCost = ExistingDihedralSum(lost);
            }
            var totalCost =
                this.CombineErrorCost(remesh.OldError, remesh.NewError) +
                this.CombineDihedralAngleCost(oldAngleCost, newAngleCost) +
                this.CombineShapeCost(remesh.OldShapeCost, remesh.NewShapeCost);
            collapse = new Collapse(kept, lost, remesh, fan, totalCost);
            return true;
        }

        private bool TryRemesh(Edge edge, Vertex kept, Vertex lost, out Remesh remesh, out List<Vertex> fan)
        {
            remesh = null;
            fan = null;
            if (edge != null && edge.CanCollapse && TryCreateRemesh(kept, lost, out fan, out var triangles))
            {
                var newError = RemeshError(lost, triangles, out var oldError, out var originalPoints);
                if (newError < this.MaxErrorSquared)
                {
                    var newShapeCost = 0.0;
                    var oldShapeCost = 0.0;
                    if (this.ShapeCost != null)
                    {
                        oldShapeCost = ExistingShapeSum(lost);
                        if (!RemeshShapeSum(triangles, out newShapeCost))
                        {
                            return false;
                        }
                    }
                    remesh = new Remesh(triangles, originalPoints, newError, oldError, newShapeCost, oldShapeCost);
                    return true;
                }
            }
            return false;
        }

        private bool TryCreateRemesh(Vertex kept, Vertex lost, out List<Vertex> fan, out VertexTriple[] remesh)
        {
            if (boundaryVertices.Contains(lost))
            {
                fan = null;
                remesh = null;
                return false;
            }
            fan = lost.FanFrom(kept);
            remesh = new VertexTriple[lost.T.Count - 2];
            var a = fan[0];
            for (var i = 1; i < fan.Count - 1; ++i)
            {
                var b = fan[i];
                var c = fan[i + 1];
                if (((b.P - a.P) ^ (c.P - a.P)).LengthSquared <= this.NormalToleranceSquare)
                {
                    return false;
                }
                remesh[i - 1] = new VertexTriple(a, b, c, new TriangleSurfaceTest(a.P, b.P, c.P, t2: this.NormalToleranceSquare));
            }
            return true;
        }

        public delegate double CombineCost(double oldValue, double newValue);

        public CombineCost CombineErrorCost { get; set; } = (o, n) => 0;

        private double RemeshError(Vertex lost, VertexTriple[] remesh, out double oldError, out OriginalPoints[] points)
        {
            var pointLists = new List<Vector3>[remesh.Length];
            var maxErrors = new double[remesh.Length];
            for (var i = 0; i < remesh.Length; ++i)
            {
                pointLists[i] = new List<Vector3>();
            }

            // Assign lost point itself
            var minError = double.PositiveInfinity;
            var minIndex = -1;
            for (var i = 0; i < remesh.Length; ++i)
            {
                var error = remesh[i].Surface.DistanceSquared(lost.P);
                if (error < minError)
                {
                    minError = error;
                    minIndex = i;
                }
            }
            pointLists[minIndex].Add(lost.P);
            maxErrors[minIndex] = minError;
            var newError = minError;

            // Assign accumulated points
            oldError = 0.0;
            foreach (var t in lost.T)
            {
                var op = originalPoints[t];
                oldError = Math.Max(oldError, op.Error);
                foreach (var p in op.Points)
                {
                    minError = double.PositiveInfinity;
                    minIndex = -1;
                    for (var i = 0; i < remesh.Length; ++i)
                    {
                        var error = remesh[i].Surface.DistanceSquared(p);
                        if (error < minError)
                        {
                            minError = error;
                            minIndex = i;
                        }
                    }
                    pointLists[minIndex].Add(p);
                    maxErrors[minIndex] = Math.Max(maxErrors[minIndex], minError);
                    newError = Math.Max(newError, minError);
                }
            }

            points = new OriginalPoints[remesh.Length];
            for (var i = 0; i < remesh.Length; ++i)
            {
                points[i] = new OriginalPoints(pointLists[i].ToArray(), maxErrors[i]);
            }

            return newError;
        }

        public double MinDihedralCosine { get; set; } = -0.5;
        public Func<double, double> DihedralAngleCost { get; set; } = x => -x;
        public CombineCost CombineDihedralAngleCost { get; set; } = (o, n) => n - o;

        private bool RemeshDihedralSum(Vertex lost, List<Vertex> fan, VertexTriple[] remesh, out double newTotal)
        {
            newTotal = 0.0;
            // Inside edges
            for (var i = 1; i < remesh.Length - 1; ++i)
            {
                var d = remesh[i].Surface.Normal * remesh[i + 1].Surface.Normal;
                if (d < this.MinDihedralCosine)
                {
                    return false;
                }
                newTotal += this.DihedralAngleCost(d);
            }
            // First
            var edge = this.Mesh.E[new Edge(fan[0], fan[1])];
            var T = edge.T.First.Value.Contains(lost) ? edge.T.Last.Value : edge.T.First.Value;
            var dot = T.N * remesh[0].Surface.Normal;
            if (dot < this.MinDihedralCosine)
            {
                return false;
            }
            newTotal += this.DihedralAngleCost(dot);
            // Main fan
            for (var i = 0; i < remesh.Length; ++i)
            {
                edge = this.Mesh.E[new Edge(remesh[i].B, remesh[i].C)];
                T = edge.T.First.Value.Contains(lost) ? edge.T.Last.Value : edge.T.First.Value;
                dot = T.N * remesh[i].Surface.Normal;
                if (dot < this.MinDihedralCosine)
                {
                    return false;
                }
                newTotal += this.DihedralAngleCost(dot);
            }
            // Last
            edge = this.Mesh.E[new Edge(fan[^1], fan[0])];
            T = edge.T.First.Value.Contains(lost) ? edge.T.Last.Value : edge.T.First.Value;
            dot = T.N * remesh[^1].Surface.Normal;
            if (dot < this.MinDihedralCosine)
            {
                return false;
            }
            newTotal += this.DihedralAngleCost(dot);

            return true;
        }

        private double ExistingDihedralSum(Vertex lost)
        {
            var oldTotal = 0.0;
            foreach (var t in lost.T)
            {
                oldTotal += this.DihedralAngleCost(t.Opposite(lost).DihedralAngleCosine);
            }
            foreach (var e in lost.E)
            {
                oldTotal += this.DihedralAngleCost(e.DihedralAngleCosine);
            }
            return oldTotal;
        }

        public double MaxShapeCost { get; set; } = double.PositiveInfinity;
        public Func<Vector3, Vector3, Vector3, double> ShapeCost { get; set; }
        public CombineCost CombineShapeCost { get; set; } = (o, n) => 0;

        private double ExistingShapeSum(Vertex lost)
        {
            var oldTotal = 0.0;
            foreach (var t in lost.T)
            {
                oldTotal += this.ShapeCost(t.A.P, t.B.P, t.C.P);
            }
            return oldTotal;
        }

        private bool RemeshShapeSum(VertexTriple[] remesh, out double newTotal)
        {
            newTotal = 0.0;
            foreach (var t in remesh)
            {
                var cost = this.ShapeCost(t.A.P, t.B.P, t.C.P);
                if (cost > this.MaxShapeCost)
                {
                    return false;
                }
                newTotal += cost;
            }
            return true;
        }

        private async Task DecimateLocal(Action<Edge> action, IProgress<object> progress, CancellationToken cancellationToken)
        {
            while (true)
            {
                var summary = GetSummary();
                cancellationToken.ThrowIfCancellationRequested();
                var startCount = this.Mesh.T.Count;
                var candidates = this.Mesh.E.Values.ToArray();
                progress?.Report(new DecimationPass(startCount, candidates.Length, await summary));
                foreach (var edge in candidates)
                {
                    action(edge);
                }
                if (this.Mesh.T.Count == startCount)
                {
                    return;
                }
                FinishIteration();
            }
        }

        //private async Task DecimateGreedy(IProgress<object> progress = null, CancellationToken cancellationToken = default)
        //{
        //    //if (!boundaryValid)
        //    //{
        //    //    SetBoundary();
        //    //}

        //    while (true)
        //    {
        //        //var candidates = this.Mesh.E.Values.ToArray();
        //        //var executed = 0;
        //        var summary = GetSummary();
        //        cancellationToken.ThrowIfCancellationRequested();
        //        var startCount = this.Mesh.T.Count;
        //        var candidates = this.Mesh.E.Values.ToArray();
        //        progress?.Report(new DecimationPass(startCount, candidates.Length, await summary));
        //        foreach (var edge in candidates)
        //        {
        //            TryCollapseGreedy(edge);
        //            //++executed;
        //        }
        //        //if (executed == 0)
        //        //{
        //        //    break;
        //        //}
        //        //progress?.Report(this.Mesh.T.Count);
        //        if (this.Mesh.T.Count == startCount)
        //        {
        //            return;
        //        }
        //        FinishIteration();
        //    }
        //}

        private void TryCollapseGreedy(Edge edge)
        {
            var (S, E) = (edge.S, edge.E);
            if (TryRemesh(S.EdgeTo(E), S, E, out var rA, out var fA) &&
                TryCollapse(S, E, rA, fA, out var cA))
            {
                if (TryRemesh(E.EdgeTo(S), E, S, out var rB, out var fB) &&
                    TryCollapse(E, S, rB, fB, out var cB))
                {
                    ModifyMesh(cA.Cost < cB.Cost ? cA : cB);
                }
                else
                {
                    ModifyMesh(cA);
                }
            }
            else
            {
                if (TryRemesh(E.EdgeTo(S), E, S, out var rB, out var fB) &&
                    TryCollapse(E, S, rB, fB, out var cB))
                {
                    ModifyMesh(cB);
                }
            }
        }

        //private async Task DecimateLocal(IProgress<object> progress = null, CancellationToken cancellationToken = default)
        //{
        //    //if (!boundaryValid)
        //    //{
        //    //    SetBoundary();
        //    //}

        //    //progress?.Report(this.Mesh.T.Count);
        //    while (true)
        //    {
        //        var summary = GetSummary();
        //        cancellationToken.ThrowIfCancellationRequested();
        //        var startCount = this.Mesh.T.Count;
        //        var candidates = this.Mesh.E.Values.ToArray();
        //        progress?.Report(new DecimationPass(startCount, candidates.Length, await summary));
        //        foreach (var edge in candidates)
        //        {
        //            //var collapse = GetBest(edge);
        //            //if (collapse != null)
        //            //{
        //            //    Execute(collapse);
        //            //}
        //            if (GetBest(edge) is Collapse collapse)
        //            {
        //                Execute(collapse);
        //            }
        //        }
        //        if (this.Mesh.T.Count == startCount)
        //        {
        //            return;
        //        }
        //        //progress?.Report(this.Mesh.T.Count);
        //        FinishIteration();
        //    }
        //}

        private void TryCollapseLocal(Edge edge)
        {
            if (GetBest(edge) is Collapse collapse)
            {
                Execute(collapse);
            }
        }

        public int LocalIterations { get; set; } = 1;

        private Collapse GetBest(Edge edge)
        {
            var collapse = GetBestLocal(edge);
            var iterations = 0;
            while (collapse != null && iterations < this.LocalIterations)
            {
                var bestEdge = collapse.Kept.EdgeTo(collapse.Lost);
                if (bestEdge == null)
                {
                    return null;
                }
                else if (bestEdge == edge)
                {
                    return collapse;
                }
                else
                {
                    edge = bestEdge;
                    collapse = GetBestLocal(bestEdge);
                    iterations++;
                }
            }
            return collapse;
        }

        private Collapse GetBestLocal(Edge edge)
        {
            var neighbours = new List<Edge>(edge.S.E.Count + edge.E.E.Count);
            neighbours.AddRange(edge.S.E);
            neighbours.AddRange(edge.E.E);
            neighbours.Remove(edge);
            var permissible = neighbours.Select(e => GetCollapse(e)).Where(c => c != null);
            return permissible.ArgMin(c => c.Cost, out var collapse, out var cost) ? collapse : null;
        }

        private Collapse GetCollapse(Edge edge)
        {
            return GetCollapse(edge.S, edge.E) is Collapse a
                ? GetCollapse(edge.E, edge.S) is Collapse b ? a.Cost < b.Cost ? a : b : a
                : GetCollapse(edge.E, edge.S) is Collapse B ? B : null;
        }

        private Collapse GetCollapse(Vertex kept, Vertex lost)
        {
            if (remeshing.Contains((kept, lost)))
            {
                remeshing.Remove((kept, lost));
                recosting.Remove((kept, lost));
                if (!TryRemesh(kept.EdgeTo(lost), kept, lost, out var remesh, out var fan))
                {
                    collapses.Remove((kept, lost));
                    return null;
                }
                if (TryCollapse(kept, lost, remesh, fan, out var collapse))
                {
                    collapses[(kept, lost)] = collapse;
                    return collapse;
                }
                else
                {
                    collapses[(kept, lost)] = new Collapse(kept, lost, remesh, fan, double.PositiveInfinity);
                    return null;
                }
            }
            else
            {
                if (collapses.TryGetValue((kept, lost), out var collapse))
                {
                    if (recosting.Contains((kept, lost)))
                    {
                        recosting.Remove((kept, lost));
                        if (TryCollapse(collapse.Kept, collapse.Lost, collapse.Remesh, collapse.Fan, out var updated))
                        {
                            collapses[(kept, lost)] = updated;
                            return updated;
                        }
                        else
                        {
                            collapses[(kept, lost)] = collapse with { Cost = double.PositiveInfinity };
                            return null;
                        }
                    }
                    return collapse.Cost != double.PositiveInfinity ? collapse : null;
                }
            }
            return null;
        }

        //private void FilterCache()
        //{
        //    // Check that we aren't going to add invalid remeshing or recosting vertices
        //    remeshing = remeshing
        //        .Where(r => this.Mesh.V.ContainsKey(r.kept.P) && this.Mesh.V.ContainsKey(r.lost.P))
        //        .ToHashSet();
        //    recosting = recosting
        //        .Where(r => this.Mesh.V.ContainsKey(r.kept.P) && this.Mesh.V.ContainsKey(r.lost.P))
        //        .ToHashSet();
        //}

        public static async Task<Decimation> Decimate(
            Action<Decimation> finalConfiguration, Action<Decimation> chunkConfiguration,
            IEnumerable<Mesh> chunks, int maxConcurrency,
            IProgress<object> progress = null, CancellationToken cancellationToken = default)
        {
            using var semaphore = new SemaphoreSlim(1);
            var finalDecimation = new Decimation(new Mesh());
            finalConfiguration(finalDecimation);
            await chunks.RunAsync(
                async chunk =>
                {
                    var chunkDecimation = new Decimation(chunk);
                    chunkConfiguration(chunkDecimation);
                    await chunkDecimation.Decimate(progress, cancellationToken);
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        finalDecimation.Merge(chunkDecimation);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, maxConcurrency, false, cancellationToken);
            finalDecimation.RemeshAll();
            await finalDecimation.Decimate(progress, cancellationToken);
            return finalDecimation;
        }
    }
}