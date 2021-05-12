using System.Collections.Generic;

namespace Vascular.Geometry.Graphs
{
    /// <summary>
    /// A graph formed by vertices and edges.
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TEdge"></typeparam>
    public class Graph<TVertex, TEdge>
        where TVertex : Vertex<TVertex, TEdge>, new()
        where TEdge : Edge<TVertex, TEdge>, new()
    {
        /// <summary>
        /// 
        /// </summary>
        public Graph()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nv"></param>
        /// <param name="ne"></param>
        /// <param name="eq"></param>
        public Graph(int nv, int ne, IEqualityComparer<TEdge> eq = null)
        {
            V = new(nv);
            E = new(ne, eq ?? Edge<TVertex, TEdge>.Undirected);
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Vector3, TVertex> V = new();

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<TEdge, TEdge> E = new(Edge<TVertex, TEdge>.Undirected);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public TVertex AddVertex(Vector3 p)
        {
            if (!V.TryGetValue(p, out var v))
            {
                v = new TVertex()
                {
                    P = p
                };
                V[p] = v;
            }
            return v;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="p"></param>
        public void MoveVertex(TVertex v, Vector3 p)
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
        public TEdge AddEdge(TEdge e)
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
        public void RemoveEdge(TEdge e)
        {
            E.Remove(e);
            e.S.E.Remove(e);
            e.E.E.Remove(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        public void RemoveVertex(TVertex v)
        {
            V.Remove(v.P);
            foreach (var e in v.E)
            {
                RemoveEdge(e);
            }
        }
    }
}
