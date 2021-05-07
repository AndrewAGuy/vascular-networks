using System.Collections.Generic;

namespace Vascular.Geometry.Graphs
{
    /// <summary>
    /// A graph formed by vertices and edges.
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Vector3, Vertex> V = new Dictionary<Vector3, Vertex>();

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Edge, Edge> E = new Dictionary<Edge, Edge>(Edge.Undirected);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Vertex AddVertex(Vector3 p)
        {
            if (!V.TryGetValue(p, out var v))
            {
                v = new Vertex(p);
                V[p] = v;
            }
            return v;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="p"></param>
        public void MoveVertex(Vertex v, Vector3 p)
        {
            V.Remove(v.P);
            V[p] = v;
            v.P = p;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void RemoveEdge(Edge e)
        {
            E.Remove(e);
            e.S.E.Remove(e);
            e.E.E.Remove(e);
        }
    }
}
