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
    public record DecimationBegin(int BoundaryVertices, int Vertices, Summary Error);
    public record DecimationStep(int Triangles, int Edges, int CandidateEdges, Summary Error);

    public class Decimation
    {
        public double MaxErrorSquared { get; set; } = 1.0e-2;
        public double NormalToleranceSquare { get; set; } = 1.0e-12;
        public double MinDihedralCosine { get; set; } = -0.5;
        public bool SubtractOldDihedral { get; set; } = true;
        public Mesh Mesh { get; }
        public int TaskCount { get; set; } = 1;

        public IEnumerable<int> SummaryMoments { get; set; }
        public IEnumerable<double> SummaryOrders { get; set; }

        public Decimation(Mesh mesh)
        {
            this.Mesh = mesh;
            originalPoints = mesh.T.ToDictionary(t => t, t => new OriginalPoints(Array.Empty<Vector3>(), 0.0));
            candidateEdges = mesh.E.Values.ToHashSet();
            boundaryVertices = mesh.V.Values.Where(v => !v.IsInterior).ToHashSet();
            if (this.ExtendBoundary)
            {
                foreach (var v in boundaryVertices.ToList())
                {
                    foreach (var V in v.UnorderedFan)
                    {
                        boundaryVertices.Add(V);
                    }
                }
            }
        }

        private void SetBoundary()
        {
            boundaryVertices.Clear();

        }

        public bool ExtendBoundary { get; set; } = true;

        public void Decimate()
        {
            while (candidateEdges.Count != 0)
            {
                var candidateCollapses = GetCandidates();
                var visited = new HashSet<Edge>(this.Mesh.E.Count);
                foreach (var collapse in candidateCollapses)
                {
                    if (!visited.Contains(new Edge(collapse.Kept, collapse.Lost)))
                    {
                        Execute(collapse, visited);
                    }
                }
            }
        }

        public record ProgressData(int Triangles, int Edges, int CandidateEdges, double MaxError, double MinError);

        public async Task DecimateAsync(IProgress<ProgressData> progress = null, CancellationToken cancellationToken = default)
        {
            while (candidateEdges.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var candidateCount = candidateEdges.Count;

                var candidateTask = GetCandidatesAsync();

                if (progress != null)
                {
                    var maxError = originalPoints.Values.Max(op => op.Error);
                    var minError = originalPoints.Values.Min(op => op.Error);
                    progress.Report(new ProgressData(this.Mesh.T.Count, this.Mesh.E.Count, candidateCount, maxError, minError));
                }
                var visited = new HashSet<Edge>(this.Mesh.E.Count);

                var candidateCollapses = await candidateTask;
                foreach (var collapse in candidateCollapses)
                {
                    if (!visited.Contains(new Edge(collapse.Kept, collapse.Lost)))
                    {
                        Execute(collapse, visited);
                    }
                }
            }
        }

        public void Merge(Decimation other)
        {
            // Save boundaries for later
            var boundary = this.Mesh.V.Values.Where(v => !v.IsInterior).ToList();
            var otherBoundary = other.Mesh.V.Values.Where(v => !v.IsInterior).ToList();
            boundaryVertices.Clear();
            // Merge meshes
            foreach (var t in other.Mesh.T)
            {
                var T = this.Mesh.AddTriangle(t.A.P, t.B.P, t.C.P);
                originalPoints[T] = other.originalPoints[t];
            }
            // Now for each vertex in the old boundary, if it is now interior add
            foreach (var b in boundary)
            {
                if (b.IsInterior)
                {
                    foreach (var e in b.E)
                    {
                        candidateEdges.Add(e);
                    }
                }
                else
                {
                    boundaryVertices.Add(b);
                }
            }
            // Same for other boundary, but this time it has to be translated into this mesh
            foreach (var b in otherBoundary)
            {
                if (this.Mesh.GetVertex(b.P) is Vertex v)
                {
                    if (v.IsInterior)
                    {
                        foreach (var e in v.E)
                        {
                            candidateEdges.Add(e);
                        }
                    }
                    else
                    {
                        boundaryVertices.Add(v);
                    }
                }
            }

            if (this.ExtendBoundary)
            {
                foreach (var v in boundaryVertices.ToList())
                {
                    foreach (var V in v.UnorderedFan)
                    {
                        boundaryVertices.Add(V);
                    }
                }
            }
        }

        private record OriginalPoints(Vector3[] Points, double Error);
        private record VertexTriple(Vertex A, Vertex B, Vertex C, TriangleSurfaceTest Surface);
        private record CollapseData(Vertex Kept, Vertex Lost, VertexTriple[] Remesh, OriginalPoints[] Points, double Cost);

        private readonly Dictionary<Triangle, OriginalPoints> originalPoints;
        private readonly HashSet<Edge> candidateEdges;
        private readonly HashSet<Vertex> boundaryVertices;

        private void Execute(CollapseData collapse, HashSet<Edge> visited)
        {
            // Update visited. All edges attached to fan of lost are invalid.
            foreach (var e in collapse.Lost.E)
            {
                var other = e.Other(collapse.Lost);
                foreach (var ee in other.E)
                {
                    visited.Add(ee);
                }
            }
            // Lost edges removed from candidates
            foreach (var e in collapse.Lost.E)
            {
                candidateEdges.Remove(e);
            }
            // Modify mesh and cost structure
            var removedTriangles = collapse.Lost.T.ToList();
            foreach (var t in removedTriangles)
            {
                originalPoints.Remove(t);
                this.Mesh.RemoveTriangle(t);
            }
            for (var i = 0; i < collapse.Remesh.Length; ++i)
            {
                var t = collapse.Remesh[i];
                var T = this.Mesh.AddTriangle(t.A.P, t.B.P, t.C.P);
                originalPoints[T] = collapse.Points[i];
            }
            // Update candidates. All edges attached to fan of kept are valid
            foreach (var e in collapse.Kept.E)
            {
                var other = e.Other(collapse.Kept);
                foreach (var ee in other.E)
                {
                    candidateEdges.Add(ee);
                }
            }
        }

        private IOrderedEnumerable<CollapseData> GetCandidates()
        {
            var candidateCollapses = new List<CollapseData>(candidateEdges.Count);
            var candidateRemovals = new List<Edge>(candidateEdges.Count);
            foreach (var edge in candidateEdges)
            {
                if (TryCreateCollapse(edge, out var collapse) && collapse != null)
                {
                    candidateCollapses.Add(collapse);
                }
                else
                {
                    candidateRemovals.Add(edge);
                }
            }
            foreach (var edge in candidateRemovals)
            {
                candidateEdges.Remove(edge);
            }
            return candidateCollapses.OrderByDescending(collapse => collapse.Cost);
        }

        private async Task<IOrderedEnumerable<CollapseData>> GetCandidatesAsync()
        {
            if (this.TaskCount == 1)
            {
                return GetCandidates();
            }

            var candidateCollapses = new List<CollapseData>(candidateEdges.Count);
            var candidateRemovals = new List<Edge>(candidateEdges.Count);
            var perThread = Math.Max(1, candidateEdges.Count / this.TaskCount);
            var tasks = new List<Task<(List<CollapseData> c, List<Edge> r)>>(this.TaskCount);
            var candidateEdgeList = candidateEdges.ToList();
            foreach (var i in Enumerable.Range(0, Math.Min(this.TaskCount, candidateEdges.Count)))
            {
                var begin = i * perThread;
                var end = i == this.TaskCount - 1 ? candidateEdgeList.Count : begin + perThread;
                tasks.Add(Task.Run(() =>
                {
                    var c = new List<CollapseData>(end - begin);
                    var r = new List<Edge>(end - begin);
                    for (var j = begin; j < end; ++j)
                    {
                        if (TryCreateCollapse(candidateEdgeList[j], out var collapse))
                        {
                            c.Add(collapse);
                        }
                        else
                        {
                            r.Add(candidateEdgeList[j]);
                        }
                    }
                    return (c, r);
                }));
            }
            while (tasks.Count > 0)
            {
                var finished = await Task.WhenAny(tasks);
                if (finished.IsFaulted)
                {
                    throw new Exception("", finished.Exception);
                }
                tasks.Remove(finished);
                candidateCollapses.AddRange(finished.Result.c);
                candidateRemovals.AddRange(finished.Result.r);
            }

            foreach (var edge in candidateRemovals)
            {
                candidateEdges.Remove(edge);
            }
            return candidateCollapses.OrderByDescending(collapse => collapse.Cost);
        }

        private bool TryCreateCollapse(Edge edge, out CollapseData collapse)
        {
            collapse = null;
            if (!edge.CanCollapse)
            {
                return false;
            }

            if (TryCreateRemesh(edge.S, edge.E, out var fanE, out var remeshE) &&
                RemeshError(edge.E, remeshE, out var oldErrorE, out var pointsE) <= this.MaxErrorSquared &&
                RemeshDihedralSum(edge.E, fanE, remeshE, out var dhSumE))
            {
                dhSumE -= this.SubtractOldDihedral ? ExistingDihedralSum(edge.E) : 0.0;
                if (TryCreateRemesh(edge.E, edge.S, out var fanS, out var remeshS) &&
                    RemeshError(edge.S, remeshS, out var oldErrorS, out var pointsS) <= this.MaxErrorSquared &&
                    RemeshDihedralSum(edge.S, fanS, remeshS, out var dhSumS))
                {
                    dhSumS -= this.SubtractOldDihedral ? ExistingDihedralSum(edge.S) : 0.0;
                    collapse = dhSumE > dhSumS
                        ? new CollapseData(edge.S, edge.E, remeshE, pointsE, dhSumE)
                        : new CollapseData(edge.E, edge.S, remeshS, pointsS, dhSumS);
                }
                else
                {
                    collapse = new CollapseData(edge.S, edge.E, remeshE, pointsE, dhSumE);
                }
            }
            else
            {
                if (TryCreateRemesh(edge.E, edge.S, out var fanS, out var remeshS) &&
                    RemeshError(edge.S, remeshS, out var oldErrorS, out var pointsS) <= this.MaxErrorSquared &&
                    RemeshDihedralSum(edge.S, fanS, remeshS, out var dhSumS))
                {
                    dhSumS -= this.SubtractOldDihedral ? ExistingDihedralSum(edge.S) : 0.0;
                    collapse = new CollapseData(edge.E, edge.S, remeshS, pointsS, dhSumS);
                }
                else
                {
                    return false;
                }
            }
            return true;
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
                newTotal += d;
            }
            // First
            var edge = this.Mesh.E[new Edge(fan[0], fan[1])];
            var T = edge.T.First.Value.Contains(lost) ? edge.T.Last.Value : edge.T.First.Value;
            var dot = T.N * remesh[0].Surface.Normal;
            if (dot < this.MinDihedralCosine)
            {
                return false;
            }
            newTotal += dot;
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
                newTotal += dot;
            }
            // Last
            edge = this.Mesh.E[new Edge(fan[^1], fan[0])];
            T = edge.T.First.Value.Contains(lost) ? edge.T.Last.Value : edge.T.First.Value;
            dot = T.N * remesh[^1].Surface.Normal;
            if (dot < this.MinDihedralCosine)
            {
                return false;
            }
            newTotal += dot;

            return true;
        }

        private static double ExistingDihedralSum(Vertex lost)
        {
            var oldTotal = 0.0;
            foreach (var t in lost.T)
            {
                oldTotal += t.Opposite(lost).DihedralAngleCosine;
            }
            foreach (var e in lost.E)
            {
                oldTotal += e.DihedralAngleCosine;
            }
            return oldTotal;
        }
    }
}