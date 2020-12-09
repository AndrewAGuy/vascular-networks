using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Acceleration;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.Triangulation.Remeshing
{
    public class EdgeCollapseDecimation
    {
        public double MaxErrorSquared { get; set; } = 1.0e-2;
        public double NormalToleranceSquare { get; set; } = 1.0e-12;
        public double MinDihedralCosine { get; set; } = -0.5;
        public bool SubtractOldDihedral { get; set; } = true;
        public Mesh Mesh { get; }
        public int ThreadCount { get; set; } = 1;

        public EdgeCollapseDecimation(Mesh mesh)
        {
            this.Mesh = mesh;
            originalPoints = mesh.T.ToDictionary(t => t, t => new OriginalPoints(Array.Empty<Vector3>(), 0.0));
            candidateEdges = mesh.E.Values.ToHashSet();
        }

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

        public record ProgressData(int Triangles, int Edges, int CandidateEdges);

        public async Task DecimateAsync(CancellationToken cancellationToken = default, IProgress<ProgressData> progress = null)
        {
            while (candidateEdges.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new ProgressData(this.Mesh.T.Count, this.Mesh.E.Count, candidateEdges.Count));
                var candidateTask = GetCandidatesAsync();
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

        private void Merge(EdgeCollapseDecimation other)
        {
            // Save boundaries for later
            var boundary = this.Mesh.V.Values.Where(v => !v.IsInterior).ToList();
            var otherBoundary = other.Mesh.V.Values.Where(v => !v.IsInterior).ToList();
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
            }
            // Same for other boundary, but this time it has to be translated into this mesh
            foreach (var b in otherBoundary)
            {
                if (this.Mesh.GetVertex(b.P) is Vertex v && v.IsInterior)
                {
                    foreach (var e in v.E)
                    {
                        candidateEdges.Add(e);
                    }
                }
            }
        }

        private record OriginalPoints(Vector3[] Points, double Error);
        private record VertexTriple(Vertex A, Vertex B, Vertex C, TriangleSurfaceTest Surface);
        private record CollapseData(Vertex Kept, Vertex Lost, VertexTriple[] Remesh, OriginalPoints[] Points, double Cost);

        private readonly Dictionary<Triangle, OriginalPoints> originalPoints;
        private readonly HashSet<Edge> candidateEdges;

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
            var candidateCollapses = new List<CollapseData>(candidateEdges.Count);
            var candidateRemovals = new List<Edge>(candidateEdges.Count);
            var perThread = candidateEdges.Count / this.ThreadCount;
            var tasks = new List<Task<(List<CollapseData> c, List<Edge> r)>>(this.ThreadCount);
            var candidateEdgeList = candidateEdges.ToList();
            foreach (var i in Enumerable.Range(0, this.ThreadCount))
            {
                var begin = i * perThread;
                var end = i == this.ThreadCount - 1 ? candidateEdgeList.Count : begin + perThread;
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
            if (!lost.IsInterior)
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

    public class EdgeCollapseDecimator
    {
        private Dictionary<Triangle, List<Vector3>> originalPoints;
        private HashSet<Edge> candidateEdges;
        public double MaxDistanceSquared { get; set; }
        public double ErrorFactor { get; set; } = 0;
        public double AngleFactor { get; set; } = 1;

        public double SquareTolerance { get; set; } = 1.0e-12;
        public double MinDihedralCosine { get; set; } = -0.5;

        public Action<int> OnIteration { get; set; }

        public void Decimate(Mesh mesh)
        {
            //originalPoints = new Dictionary<Triangle, List<Vector3>>(mesh.T.Count);
            candidateEdges = mesh.E.Values.ToHashSet();
            originalPoints = mesh.T.ToDictionary(t => t, t => new List<Vector3>());
            while (candidateEdges.Count != 0)
            {
                this.OnIteration(mesh.T.Count);
                var candidateCollapses = GetCandidates(mesh);
                var visited = new HashSet<Edge>(mesh.E.Count);
                foreach (var collapse in candidateCollapses)
                {
                    if (!visited.Contains(new Edge(collapse.kept, collapse.lost)))
                    {
                        Execute(collapse, visited, mesh);
                    }
                }
            }
        }

        private void Execute(Collapse collapse, HashSet<Edge> visited, Mesh mesh)
        {
            // Update visited. All edges attached to fan of lost are invalid.
            foreach (var e in collapse.lost.E)
            {
                var other = e.Other(collapse.lost);
                foreach (var ee in other.E)
                {
                    visited.Add(ee);
                }
            }
            // Modify mesh and cost structure
            var removedTriangles = collapse.lost.T.ToList();
            foreach (var t in removedTriangles)
            {
                originalPoints.Remove(t);
                mesh.RemoveTriangle(t);
            }
            for (var i = 0; i < collapse.tris.Length; ++i)
            {
                var t = collapse.tris[i];
                var T = mesh.AddTriangle(t.a.P, t.b.P, t.c.P);
                originalPoints[T] = collapse.points[i];
            }
            // Update candidates. All edges attached to fan of kept are valid
            foreach (var e in collapse.kept.E)
            {
                var other = e.Other(collapse.kept);
                foreach (var ee in other.E)
                {
                    candidateEdges.Add(ee);
                }
            }
        }

        private record OriginalPoints(Vector3[] Points, double Error);
        private record Triple(Vertex a, Vertex b, Vertex c, TriangleSurfaceTest surface);
        private record Collapse(Vertex kept, Vertex lost, Triple[] tris, List<Vector3>[] points, double cost);

        private IOrderedEnumerable<Collapse> GetCandidates(Mesh mesh)
        {
            var candidateCollapses = new List<Collapse>(candidateEdges.Count);
            var candidateRemovals = new List<Edge>(candidateEdges.Count);
            foreach (var edge in candidateEdges)
            {
                if (TryCreateCollapse(edge, mesh, out var collapse) && collapse != null)
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
            return candidateCollapses.OrderBy(collapse => collapse.cost);
        }

        private bool TryCreateCollapse(Edge edge, Mesh mesh, out Collapse collapse)
        {
            collapse = null;
            //try
            //{
            if (!edge.CanCollapse)
            {
                return false;
            }
            // Evaluate both potential collapses
            var c1 = EdgeCollapseRemesh(edge.S, edge.E);
            var c2 = EdgeCollapseRemesh(edge.E, edge.S);
            if (c1.ok)
            {
                var e1 = RemeshErrorSquared(edge.E, c1.remesh);
                if (c2.ok)
                {
                    var e2 = RemeshErrorSquared(edge.S, c2.remesh);
                    if (e1.error <= this.MaxDistanceSquared)
                    {
                        if (e2.error <= this.MaxDistanceSquared)
                        {
                            var C1 = RemeshDihedralCost(edge.E, c1.fan, c1.remesh, mesh);
                            var C2 = RemeshDihedralCost(edge.S, c2.fan, c2.remesh, mesh);
                            collapse = C2.cost < C1.cost
                                ? new Collapse(edge.E, edge.S, c2.remesh, e2.points, C2.cost)
                                : new Collapse(edge.S, edge.E, c1.remesh, e1.points, C1.cost);
                        }
                        else
                        {
                            var C1 = RemeshDihedralCost(edge.E, c1.fan, c1.remesh, mesh);
                            collapse = new Collapse(edge.S, edge.E, c1.remesh, e1.points, C1.cost);
                        }
                    }
                    else if (e2.error <= this.MaxDistanceSquared)
                    {
                        var C2 = RemeshDihedralCost(edge.S, c2.fan, c2.remesh, mesh);
                        collapse = new Collapse(edge.E, edge.S, c2.remesh, e2.points, C2.cost);
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (e1.error <= this.MaxDistanceSquared)
                {
                    var C1 = RemeshDihedralCost(edge.E, c1.fan, c1.remesh, mesh);
                    collapse = new Collapse(edge.S, edge.E, c1.remesh, e1.points, C1.cost);
                }
                else
                {
                    return false;
                }
            }
            else if (c2.ok)
            {
                var e2 = RemeshErrorSquared(edge.S, c2.remesh);
                if (e2.error <= this.MaxDistanceSquared)
                {
                    var C2 = RemeshDihedralCost(edge.S, c2.fan, c2.remesh, mesh);
                    collapse = new Collapse(edge.E, edge.S, c2.remesh, e2.points, C2.cost);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            //var e1 = RemeshErrorSquared(edge.E, c1.remesh);
            //var e2 = RemeshErrorSquared(edge.S, c2.remesh);
            //if (e1.error <= this.MaxDistanceSquared)
            //{
            //    if (e2.error <= this.MaxDistanceSquared)
            //    {
            //        var C1 = RemeshDihedralCost(edge.E, c1.fan, c1.remesh, mesh);
            //        var C2 = RemeshDihedralCost(edge.S, c2.fan, c2.remesh, mesh);
            //        collapse = C2 < C1
            //            ? new Collapse(edge.E, edge.S, c2.remesh, e2.points, C2)
            //            : new Collapse(edge.S, edge.E, c1.remesh, e1.points, C1);
            //    }
            //    else
            //    {
            //        var C1 = RemeshDihedralCost(edge.E, c1.fan, c1.remesh, mesh);
            //        collapse = new Collapse(edge.S, edge.E, c1.remesh, e1.points, C1);
            //    }
            //}
            //else if (e2.error <= this.MaxDistanceSquared)
            //{
            //    var C2 = RemeshDihedralCost(edge.S, c2.fan, c2.remesh, mesh);
            //    collapse = new Collapse(edge.E, edge.S, c2.remesh, e2.points, C2);
            //}
            //else
            //{
            //    return false;
            //}
            return true;
            //}
            //catch (Exception)
            //{
            //    return false;
            //}
        }

        private (double cost, bool ok) RemeshDihedralCost(Vertex lost, List<Vertex> fan, Triple[] remesh, Mesh mesh)
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

            var newTotal = 0.0;
            for (var i = 1; i < remesh.Length - 1; ++i)
            {
                var d = remesh[i].surface.Normal * remesh[i + 1].surface.Normal;
                if (d < this.MinDihedralCosine)
                {
                    return (double.PositiveInfinity, false);
                }
                newTotal += d;
            }
            // First and last special
            var edge = mesh.E[new Edge(fan[0], fan[1])];
            var T = edge.T.First.Value.Contains(lost) ? edge.T.Last.Value : edge.T.First.Value;
            var dot = T.N * remesh[0].surface.Normal;
            if (dot < this.MinDihedralCosine)
            {
                return (double.PositiveInfinity, false);
            }
            newTotal += dot;
            for (var i = 0; i < remesh.Length; ++i)
            {
                edge = mesh.E[new Edge(remesh[i].b, remesh[i].c)];
                T = edge.T.First.Value.Contains(lost) ? edge.T.Last.Value : edge.T.First.Value;
                dot = T.N * remesh[i].surface.Normal;
                if (dot < this.MinDihedralCosine)
                {
                    return (double.PositiveInfinity, false);
                }
                newTotal += dot;
            }
            edge = mesh.E[new Edge(fan[^1], fan[0])];
            T = edge.T.First.Value.Contains(lost) ? edge.T.Last.Value : edge.T.First.Value;
            dot = T.N * remesh[^1].surface.Normal;
            if (dot < this.MinDihedralCosine)
            {
                return (double.PositiveInfinity, false);
            }
            newTotal += dot;

            return (oldTotal - newTotal, true);
        }

        private (List<Vertex> fan, Triple[] remesh, bool ok) EdgeCollapseRemesh(Vertex kept, Vertex lost)
        {
            // Walk fan starting from kept vertex, testing dihedral angles
            var fan = lost.FanFrom(kept);
            var remesh = new Triple[lost.T.Count - 2];
            var a = fan[0];
            for (var i = 1; i < fan.Count - 1; ++i)
            {
                var b = fan[i];
                var c = fan[i + 1];
                if (((b.P - a.P) ^ (c.P - a.P)).LengthSquared < this.SquareTolerance)
                {
                    return (null, null, false);
                }
                remesh[i - 1] = new Triple(a, b, c, new TriangleSurfaceTest(a.P, b.P, c.P, t2: this.SquareTolerance));
            }
            return (fan, remesh, true);
        }

        private (double error, List<Vector3>[] points) RemeshErrorSquared(Vertex lost, Triple[] remesh)
        {
            var points = new List<Vector3>[remesh.Length];
            for (var i = 0; i < remesh.Length; ++i)
            {
                points[i] = new List<Vector3>();
            }

            // Assign lost point itself
            var minError = double.PositiveInfinity;
            var minIndex = -1;
            for (var i = 0; i < remesh.Length; ++i)
            {
                var error = remesh[i].surface.DistanceSquared(lost.P);
                if (error < minError)
                {
                    minError = error;
                    minIndex = i;
                }
            }
            points[minIndex].Add(lost.P);
            var maxError = minError;

            // Assign accumulated points
            foreach (var t in lost.T)
            {
                foreach (var p in originalPoints[t])
                {
                    minError = double.PositiveInfinity;
                    minIndex = -1;
                    for (var i = 0; i < remesh.Length; ++i)
                    {
                        var error = remesh[i].surface.DistanceSquared(p);
                        if (error < minError)
                        {
                            minError = error;
                            minIndex = i;
                        }
                    }
                    points[minIndex].Add(p);
                    maxError = Math.Max(maxError, minError);
                }
            }

            return (maxError, points);
        }
    }
}
