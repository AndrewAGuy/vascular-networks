using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Geometry.Graphs
{
    public class Edge
    {
        public Edge(Vertex s, Vertex e)
        {
            (S, E) = (s, e);
        }

        public Vertex S, E;

        public Vertex Other(Vertex v)
        {
            return v == S ? E : S;
        }

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

        public static IEqualityComparer<Edge> Undirected => new UndirectedComparer();
        public static IEqualityComparer<Edge> Directed => new DirectedComparer();
    }
}
