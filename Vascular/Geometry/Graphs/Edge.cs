using System;
using System.Collections.Generic;

namespace Vascular.Geometry.Graphs
{
    /// <summary>
    /// Links vertices.
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        public Edge(Vertex s, Vertex e)
        {
            (S, E) = (s, e);
        }

        /// <summary>
        /// 
        /// </summary>
        public Vertex S, E;

        /// <summary>
        /// Use only when certain that <paramref name="v"/> is either <see cref="S"/> or <see cref="E"/>.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vertex Other(Vertex v)
        {
            return v == S ? E : S;
        }

        /// <summary>
        /// Returns null if not present in edge.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vertex OtherSafe(Vertex v)
        {
            return v == S ? E : v == E ? S : null;
        }

        private class UndirectedComparer : IEqualityComparer<Edge>
        {
            public bool Equals(Edge x, Edge y)
            {
                return x.E == y.E && x.S == y.S
                    || x.E == y.S && x.S == y.E;
            }

            public int GetHashCode(Edge obj)
            {
                return obj.S.GetHashCode() ^ obj.E.GetHashCode();
            }
        }

        private class DirectedComparer : IEqualityComparer<Edge>
        {
            public bool Equals(Edge x, Edge y)
            {
                return x.S == y.S && x.E == y.E;
            }

            public int GetHashCode(Edge obj)
            {
                return HashCode.Combine(obj.S, obj.E);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static IEqualityComparer<Edge> Undirected => new UndirectedComparer();

        /// <summary>
        /// 
        /// </summary>
        public static IEqualityComparer<Edge> Directed => new DirectedComparer();
    }
}
