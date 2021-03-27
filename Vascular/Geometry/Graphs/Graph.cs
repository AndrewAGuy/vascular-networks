using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Geometry.Graphs
{
    public class Graph
    {
        public Dictionary<Vector3, Vertex> V = new Dictionary<Vector3, Vertex>();
        public Dictionary<Edge, Edge> E = new Dictionary<Edge, Edge>(Edge.Undirected);

        public Vertex AddVertex(Vector3 p)
        {
            if (!V.TryGetValue(p, out var v))
            {
                v = new Vertex(p);
                V[p] = v;
            }
            return v;
        }

        public void MoveVertex(Vertex v, Vector3 p)
        {
            V.Remove(v.P);
            V[p] = v;
            v.P = p;
        }

        public Edge AddEdge(Edge e)
        {
            if (E.TryGetValue(e, out var ee))
            {
                return ee;
            }
            E[e] = e;
            e.S.E.AddLast(e);
            e.E.E.AddLast(e);
            return e;
        }

        public void RemoveEdge(Edge e)
        {
            E.Remove(e);
            e.S.E.Remove(e);
            e.E.E.Remove(e);
        }
    }
}
