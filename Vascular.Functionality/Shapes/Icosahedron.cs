using System;
using Vascular.Geometry;
using Vascular.Geometry.Graphs;
using Vascular.Geometry.Triangulation;

namespace Vascular.Functionality.Shapes
{
    public static class Icosahedron
    {
        private static (Vector3 P, Vector3 p, Vector3[] R, Vector3[] r) VerticesUnitZ()
        {
            var P = new Vector3(0, 0, 1);
            var p = new Vector3(0, 0, -1);
            var x = Math.Cos(Math.Atan(0.5));
            var z = Math.Sin(Math.Atan(0.5));
            var b = Math.PI / 5;
            var R = new Vector3[5];
            var r = new Vector3[5];
            for (var i = 0; i < 5; ++i)
            {
                var I = 2 * i;
                R[i] = new Vector3(x * Math.Cos(b * I), x * Math.Sin(b * I), z);
                r[i] = new Vector3(x * Math.Cos(b * (I + 1)), x * Math.Sin(b * (I + 1)), -z);
            }
            return (P, p, R, r);
        }

        public static void UnitZ(Mesh mesh)
        {
            var (P, p, R, r) = VerticesUnitZ();
            for (var i = 0; i < 5; ++i)
            {
                var j = (i + 1) % 5;
                // Make the top and bottom ring
                mesh.AddTriangle(R[i], R[j], P);
                mesh.AddTriangle(r[j], r[i], p);
                // Make the intermediate rings
                mesh.AddTriangle(r[i], R[j], R[i]);
                mesh.AddTriangle(r[i], r[j], R[j]);
            }
        }

        public static Mesh Refine(Mesh mesh)
        {
            var refined = new Mesh();
            foreach (var t in mesh.T)
            {
                var a = t.A.P;
                var b = t.B.P;
                var c = t.C.P;
                var ab = (a + b).Normalize();
                var bc = (b + c).Normalize();
                var ca = (c + a).Normalize();
                refined.AddTriangle(a, ab, ca);
                refined.AddTriangle(b, bc, ab);
                refined.AddTriangle(c, ca, bc);
                refined.AddTriangle(ab, bc, ca);
            }
            return refined;
        }

        public static void UnitZ<TV, TE>(Graph<TV, TE> graph)
            where TV : Vertex<TV, TE>, new()
            where TE : Edge<TV, TE>, new()
        {
            var (P, p, R, r) = VerticesUnitZ();
            for (var i = 0; i < 5; ++i)
            {
                var J = (i + 1) % 5;
                var j = (i - 1) % 5;
                // Top and bottom connections
                graph.AddEdge(new() { S = graph.AddVertex(P), E = graph.AddVertex(R[i]) });
                graph.AddEdge(new() { S = graph.AddVertex(p), E = graph.AddVertex(r[i]) });
                // Neighbours in intermediate rings
                graph.AddEdge(new() { S = graph.AddVertex(R[i]), E = graph.AddVertex(R[J]) });
                graph.AddEdge(new() { S = graph.AddVertex(r[i]), E = graph.AddVertex(r[J]) });
                // Connections from top to bottom ring
                graph.AddEdge(new() { S = graph.AddVertex(R[i]), E = graph.AddVertex(r[i]) });
                graph.AddEdge(new() { S = graph.AddVertex(R[i]), E = graph.AddVertex(r[j]) });
            }
        }
    }
}
