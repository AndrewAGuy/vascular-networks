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

    public class Decimation
    {
        public double MaxErrorSquared { get; set; } = 0.0;
        public double NormalToleranceSquare { get; set; } = 1.0e-12;
        public bool SubtractOldDihedral { get; set; } = true;
        public Mesh Mesh { get; }
        //public int TaskCount { get; set; } = 1;

        public bool ReportErrors { get; set; }
        public IEnumerable<int> SummaryMoments { get; set; }
        public IEnumerable<double> SummaryOrders { get; set; }

        public int EdgesPerChunk { get; set; } = 1024;
        public int MaxConcurrentChunks { get; set; } = 1;

        public bool DropErrors { get; set; } = false;

        public Decimation(Mesh mesh)
        {
            this.Mesh = mesh ?? new Mesh();
            ClearErrors();
            RemeshAll();
            //candidateEdges = mesh.E.Values.ToHashSet();
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

        public bool ExtendBoundary { get; set; } = true;

        //public void Decimate()
        //{
        //    while (candidateEdges.Count != 0)
        //    {
        //        var candidateCollapses = GetCandidates();
        //        var visited = new HashSet<Edge>(this.Mesh.E.Count);
        //        foreach (var collapse in candidateCollapses)
        //        {
        //            if (!visited.Contains(new Edge(collapse.Kept, collapse.Lost)))
        //            {
        //                Execute(collapse, visited);
        //            }
        //        }
        //    }
        //}

        //public record ProgressData(int Triangles, int Edges, int CandidateEdges, double MaxError, double MinError);

        //public async Task DecimateAsync(IProgress<ProgressData> progress = null, CancellationToken cancellationToken = default)
        //{
        //    while (candidateEdges.Count != 0)
        //    {
        //        cancellationToken.ThrowIfCancellationRequested();

        //        var candidateCount = candidateEdges.Count;

        //        var candidateTask = GetCandidatesAsync();

        //        if (progress != null)
        //        {
        //            var maxError = originalPoints.Values.Max(op => op.Error);
        //            var minError = originalPoints.Values.Min(op => op.Error);
        //            progress.Report(new ProgressData(this.Mesh.T.Count, this.Mesh.E.Count, candidateCount, maxError, minError));
        //        }
        //        var visited = new HashSet<Edge>(this.Mesh.E.Count);

        //        var candidateCollapses = await candidateTask;
        //        foreach (var collapse in candidateCollapses)
        //        {
        //            if (!visited.Contains(new Edge(collapse.Kept, collapse.Lost)))
        //            {
        //                Execute(collapse, visited);
        //            }
        //        }
        //    }
        //}

        //public void Merge(Decimation other)
        //{
        //    // Save boundaries for later
        //    var boundary = this.Mesh.V.Values.Where(v => !v.IsInterior).ToArray();
        //    var otherBoundary = other.Mesh.V.Values.Where(v => !v.IsInterior).ToArray();
        //    boundaryVertices.Clear();
        //    // Merge meshes
        //    foreach (var t in other.Mesh.T)
        //    {
        //        var T = this.Mesh.AddTriangle(t.A.P, t.B.P, t.C.P);
        //        originalPoints[T] = other.originalPoints[t];
        //    }
        //    // Now for each vertex in the old boundary, if it is now interior add
        //    foreach (var b in boundary)
        //    {
        //        if (b.IsInterior)
        //        {
        //            foreach (var e in b.E)
        //            {
        //                candidateEdges.Add(e);
        //            }
        //        }
        //        else
        //        {
        //            boundaryVertices.Add(b);
        //        }
        //    }
        //    // Same for other boundary, but this time it has to be translated into this mesh
        //    foreach (var b in otherBoundary)
        //    {
        //        if (this.Mesh.GetVertex(b.P) is Vertex v)
        //        {
        //            if (v.IsInterior)
        //            {
        //                foreach (var e in v.E)
        //                {
        //                    candidateEdges.Add(e);
        //                }
        //            }
        //            else
        //            {
        //                boundaryVertices.Add(v);
        //            }
        //        }
        //    }

        //    if (this.ExtendBoundary)
        //    {
        //        foreach (var v in boundaryVertices.ToArray())
        //        {
        //            foreach (var V in v.UnorderedFan)
        //            {
        //                boundaryVertices.Add(V);
        //            }
        //        }
        //    }
        //}

        public void Merge(Decimation other)
        {
            boundaryValid = false;

            originalPoints.EnsureCapacity(originalPoints.Count + other.originalPoints.Count);
            foreach (var t in other.Mesh.T)
            {
                var T = this.Mesh.AddTriangle(t.A.P, t.B.P, t.C.P);
                originalPoints[T] = other.originalPoints[t];
            }

            collapses.EnsureCapacity(collapses.Count + other.collapses.Count);
            foreach (var c in other.collapses.Values)
            {
                var C = Translate(c);
                collapses[(C.Kept, C.Lost)] = C;
            }

            recosting.EnsureCapacity(recosting.Count + other.recosting.Count);
            foreach (var (k, l) in other.recosting)
            {
                recosting.Add((Translate(k), Translate(l)));
            }
            remeshing.EnsureCapacity(remeshing.Count + other.remeshing.Count);
            foreach (var (k, l) in other.remeshing)
            {
                remeshing.Add((Translate(k), Translate(l)));
            }
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

        //private record CollapseData(Vertex Kept, Vertex Lost, VertexTriple[] Remesh, OriginalPoints[] Points, double Cost);

        private record Collapse(Vertex Kept, Vertex Lost, Remesh Remesh, List<Vertex> Fan, double Cost);

        private Dictionary<Triangle, OriginalPoints> originalPoints;
        //private readonly HashSet<Edge> candidateEdges;
        private Dictionary<(Vertex kept, Vertex lost), Collapse> collapses;
        private HashSet<(Vertex kept, Vertex lost)> remeshing;
        private HashSet<(Vertex kept, Vertex lost)> recosting;
        private HashSet<Vertex> boundaryVertices;
        //private Dictionary<(Vertex kept, Vertex lost), Remesh> remeshes;

        public async Task Decimate(IProgress<object> progress = null, CancellationToken cancellationToken = default)
        {
            if (!boundaryValid)
            {
                SetBoundary();
            }
            progress?.Report(new DecimationBegin(boundaryVertices.Count, this.Mesh.V.Count));
            while (remeshing.Count != 0 || recosting.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remesh = remeshing.Count;
                var recost = recosting.Count;
                var summary = Task.FromResult<Summary>(null);
                if (this.ReportErrors)
                {
                    var errors = originalPoints.Values.Select(op => op.Error).ToArray();
                    summary = Task.Run(() => new Summary(errors, this.SummaryMoments, this.SummaryOrders));
                }
                //progress?.Report(new DecimationStepBegin(this.Mesh.T.Count, this.Mesh.E.Count, remeshing.Count, recosting.Count,
                //    new Summary(originalPoints.Values.Select(op => op.Error).ToArray(), this.SummaryMoments, this.SummaryOrders)));

                var valid = await GetCollapses(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new DecimationStep(this.Mesh.T.Count, this.Mesh.E.Count, remesh, recost, valid.Count(), await summary));
                //progress?.Report(new DecimationStepCandidates(valid.Count(), collapses.Count));
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
            //var perThread = Math.Max(1, remeshing.Count / this.TaskCount);
            var workingArray = remeshing.ToArray();
            //return Enumerable.Range(0, Math.Min(this.TaskCount, remeshing.Count)).RunAsync(
            //    async i =>
            return EnumerateChunks(remeshing.Count, this.EdgesPerChunk).RunAsync(
                async range =>
                {
                    var (begin, end) = range;
                    //var begin = i * perThread;
                    //var end = i == this.TaskCount - 1 ? remeshing.Count : begin + perThread;
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
                }, this.MaxConcurrentChunks, cancellationToken);
        }

        private Task GetRecosting(List<Collapse> validCollapses, CancellationToken cancellationToken)
        {
            //var perThread = Math.Max(1, recosting.Count / this.TaskCount);
            var workingArray = recosting.ToArray();
            //return Enumerable.Range(0, Math.Min(this.TaskCount, recosting.Count)).RunAsync(
            //    async i =>
            return EnumerateChunks(recosting.Count, this.EdgesPerChunk).RunAsync(
                async range =>
                {
                    var (begin, end) = range;
                    //var begin = i * perThread;
                    //var end = i == this.TaskCount - 1 ? recosting.Count : begin + perThread;
                    var valid = new List<Collapse>(end - begin);
                    var invalid = new List<Collapse>(end - begin);
                    for (var j = begin; j < end; ++j)
                    {
                        var V = workingArray[j];
                        // Test for not being in remeshing as a remesh may have occurred already and readded
                        if (!remeshing.Contains(V) && collapses.TryGetValue(V, out var old))
                        {
                            if (TryCollapse(old.Kept, old.Lost, old.Remesh, old.Fan, out var updated))
                            {
                                valid.Add(updated);
                            }
                            else
                            {
                                invalid.Add(updated);
                            }
                        }
                    }

                    // No need to update collapses, as we'd only ever see it from either another call to this
                    // where we'd update costs again and nothing else, or a call to remesh where everything
                    // would be changed
                    await validSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        validCollapses.AddRange(valid);
                    }
                    finally
                    {
                        validSemaphore.Release();
                    }
                }, this.MaxConcurrentChunks, cancellationToken);
        }

        //private async Task<IOrderedEnumerable<Collapse>> GetCollapses()
        //{
        //    // Firstly get remeshing
        //    var perThread = Math.Max(1, remeshing.Count / this.TaskCount);
        //    var workingArray = remeshing.ToArray();
        //    await Enumerable.Range(0, Math.Min(this.TaskCount, remeshing.Count)).RunAsync(
        //        i =>
        //        {
        //            var begin = i * perThread;
        //            var end = i == this.TaskCount - 1 ? remeshing.Count : begin + perThread;
        //            var C = new List<Collapse>(end - begin);
        //            var R = new List<Collapse>(end - begin);
        //            for (var j = begin; j < end; ++j)
        //            {
        //                var (kept, lost) = workingArray[j];
        //                if (TryRemesh(kept.EdgeTo(lost), kept, lost, out var remesh, out var fan))
        //                {
        //                    if (TryCollapse(kept, lost, remesh, fan, out var collapse))
        //                    {
        //                        C.Add(collapse);
        //                    }
        //                    else
        //                    {
        //                        R.Add(new Collapse(kept, lost, remesh, fan, double.PositiveInfinity));
        //                    }
        //                }

        //            }
        //            lock (collapses)
        //            {
        //                foreach (var c in C)
        //                {
        //                    collapses[(c.Kept, c.Lost)] = c;
        //                }
        //            }
        //            return Task.CompletedTask;
        //        }, this.TaskCount, this.TaskCount);
        //    // Then get recosting, ignoring if remeshed
        //    perThread = Math.Max(1, recosting.Count / this.TaskCount);
        //    workingArray = recosting.ToArray();
        //    await Enumerable.Range(0, Math.Min(this.TaskCount, recosting.Count)).RunAsync(
        //        i =>
        //        {
        //            var begin = i * perThread;
        //            var end = i == this.TaskCount - 1 ? remeshing.Count : begin + perThread;
        //            var C = new List<Collapse>(end - begin);
        //            for (var j = begin; j < end; ++j)
        //            {
        //                var V = workingArray[j];
        //                if (remeshing.Contains(V))
        //                {
        //                    continue;
        //                }
        //                var existing = collapses[V];

        //            }
        //            return Task.CompletedTask;
        //        }, this.TaskCount, this.TaskCount);

        //    return collapses.Values.OrderBy(c => c.Cost);
        //}

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

        //private void Execute(CollapseData collapse, HashSet<Edge> visited)
        //{
        //    // Update visited. All edges attached to fan of lost are invalid.
        //    foreach (var e in collapse.Lost.E)
        //    {
        //        var other = e.Other(collapse.Lost);
        //        foreach (var ee in other.E)
        //        {
        //            visited.Add(ee);
        //        }
        //    }
        //    // Lost edges removed from candidates
        //    foreach (var e in collapse.Lost.E)
        //    {
        //        candidateEdges.Remove(e);
        //    }
        //    // Modify mesh and cost structure
        //    var removedTriangles = collapse.Lost.T.ToArray();
        //    foreach (var t in removedTriangles)
        //    {
        //        originalPoints.Remove(t);
        //        this.Mesh.RemoveTriangle(t);
        //    }
        //    for (var i = 0; i < collapse.Remesh.Length; ++i)
        //    {
        //        var t = collapse.Remesh[i];
        //        var T = this.Mesh.AddTriangle(t.A.P, t.B.P, t.C.P);
        //        originalPoints[T] = collapse.Points[i];
        //    }
        //    // Update candidates. All edges attached to fan of kept are valid
        //    foreach (var e in collapse.Kept.E)
        //    {
        //        var other = e.Other(collapse.Kept);
        //        foreach (var ee in other.E)
        //        {
        //            candidateEdges.Add(ee);
        //        }
        //    }
        //}

        //private IOrderedEnumerable<CollapseData> GetCandidates()
        //{
        //    var candidateCollapses = new List<CollapseData>(candidateEdges.Count);
        //    var candidateRemovals = new List<Edge>(candidateEdges.Count);
        //    foreach (var edge in candidateEdges)
        //    {
        //        if (TryCreateCollapse(edge, out var collapse) && collapse != null)
        //        {
        //            candidateCollapses.Add(collapse);
        //        }
        //        else
        //        {
        //            candidateRemovals.Add(edge);
        //        }
        //    }
        //    foreach (var edge in candidateRemovals)
        //    {
        //        candidateEdges.Remove(edge);
        //    }
        //    return candidateCollapses.OrderByDescending(collapse => collapse.Cost);
        //}

        //private async Task<IOrderedEnumerable<CollapseData>> GetCandidatesAsync()
        //{
        //    if (this.TaskCount == 1)
        //    {
        //        return GetCandidates();
        //    }

        //    var candidateCollapses = new List<CollapseData>(candidateEdges.Count);
        //    var candidateRemovals = new List<Edge>(candidateEdges.Count);
        //    var perThread = Math.Max(1, candidateEdges.Count / this.TaskCount);
        //    var tasks = new List<Task<(List<CollapseData> c, List<Edge> r)>>(this.TaskCount);
        //    var candidateEdgeList = candidateEdges.ToArray();
        //    foreach (var i in Enumerable.Range(0, Math.Min(this.TaskCount, candidateEdges.Count)))
        //    {
        //        var begin = i * perThread;
        //        var end = i == this.TaskCount - 1 ? candidateEdgeList.Length : begin + perThread;
        //        tasks.Add(Task.Run(() =>
        //        {
        //            var c = new List<CollapseData>(end - begin);
        //            var r = new List<Edge>(end - begin);
        //            for (var j = begin; j < end; ++j)
        //            {
        //                if (TryCreateCollapse(candidateEdgeList[j], out var collapse))
        //                {
        //                    c.Add(collapse);
        //                }
        //                else
        //                {
        //                    r.Add(candidateEdgeList[j]);
        //                }
        //            }
        //            return (c, r);
        //        }));
        //    }
        //    while (tasks.Count > 0)
        //    {
        //        var finished = await Task.WhenAny(tasks);
        //        if (finished.IsFaulted)
        //        {
        //            throw new Exception("", finished.Exception);
        //        }
        //        tasks.Remove(finished);
        //        candidateCollapses.AddRange(finished.Result.c);
        //        candidateRemovals.AddRange(finished.Result.r);
        //    }

        //    foreach (var edge in candidateRemovals)
        //    {
        //        candidateEdges.Remove(edge);
        //    }
        //    return candidateCollapses.OrderByDescending(collapse => collapse.Cost);
        //}

        //private bool TryCreateCollapse(Edge edge, out CollapseData collapse)
        //{
        //    collapse = null;
        //    if (!edge.CanCollapse)
        //    {
        //        return false;
        //    }

        //    if (TryCreateRemesh(edge.S, edge.E, out var fanE, out var remeshE) &&
        //        RemeshError(edge.E, remeshE, out var oldErrorE, out var pointsE) <= this.MaxErrorSquared &&
        //        RemeshDihedralSum(edge.E, fanE, remeshE, out var dhSumE))
        //    {
        //        dhSumE -= this.SubtractOldDihedral ? ExistingDihedralSum(edge.E) : 0.0;
        //        if (TryCreateRemesh(edge.E, edge.S, out var fanS, out var remeshS) &&
        //            RemeshError(edge.S, remeshS, out var oldErrorS, out var pointsS) <= this.MaxErrorSquared &&
        //            RemeshDihedralSum(edge.S, fanS, remeshS, out var dhSumS))
        //        {
        //            dhSumS -= this.SubtractOldDihedral ? ExistingDihedralSum(edge.S) : 0.0;
        //            collapse = dhSumE > dhSumS
        //                ? new CollapseData(edge.S, edge.E, remeshE, pointsE, dhSumE)
        //                : new CollapseData(edge.E, edge.S, remeshS, pointsS, dhSumS);
        //        }
        //        else
        //        {
        //            collapse = new CollapseData(edge.S, edge.E, remeshE, pointsE, dhSumE);
        //        }
        //    }
        //    else
        //    {
        //        if (TryCreateRemesh(edge.E, edge.S, out var fanS, out var remeshS) &&
        //            RemeshError(edge.S, remeshS, out var oldErrorS, out var pointsS) <= this.MaxErrorSquared &&
        //            RemeshDihedralSum(edge.S, fanS, remeshS, out var dhSumS))
        //        {
        //            dhSumS -= this.SubtractOldDihedral ? ExistingDihedralSum(edge.S) : 0.0;
        //            collapse = new CollapseData(edge.E, edge.S, remeshS, pointsS, dhSumS);
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}

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
    }
}