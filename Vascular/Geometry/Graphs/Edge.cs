using System;
using System.Collections.Generic;

namespace Vascular.Geometry.Graphs
{
    /// <summary>
    /// Links vertices.
    /// </summary>
    public class Edge<TVertex, TEdge>
        where TVertex : Vertex<TVertex, TEdge>
        where TEdge : Edge<TVertex, TEdge>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        public Edge(TVertex s, TVertex e)
        {
            (S, E) = (s, e);
        }

        /// <summary>
        /// 
        /// </summary>
        public Edge()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public TVertex S, E;

        /// <summary>
        /// Use only when certain that <paramref name="v"/> is either <see cref="S"/> or <see cref="E"/>.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public TVertex Other(TVertex v)
        {
            return v == S ? E : S;
        }

        /// <summary>
        /// Returns null if not present in edge.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public TVertex OtherSafe(TVertex v)
        {
            return v == S ? E : v == E ? S : null;
        }

        private class UndirectedComparer : IEqualityComparer<TEdge>
        {
            public bool Equals(TEdge x, TEdge y)
            {
                return x.E == y.E && x.S == y.S
                    || x.E == y.S && x.S == y.E;
            }

            public int GetHashCode(TEdge obj)
            {
                return obj.S.GetHashCode() ^ obj.E.GetHashCode();
            }
        }

        private class DirectedComparer : IEqualityComparer<TEdge>
        {
            public bool Equals(TEdge x, TEdge y)
            {
                return x.S == y.S && x.E == y.E;
            }

            public int GetHashCode(TEdge obj)
            {
                return HashCode.Combine(obj.S, obj.E);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEqualityComparer<TEdge> Undirected => new UndirectedComparer();

        /// <summary>
        /// 
        /// </summary>
        public static IEqualityComparer<TEdge> Directed => new DirectedComparer();
    }
}
