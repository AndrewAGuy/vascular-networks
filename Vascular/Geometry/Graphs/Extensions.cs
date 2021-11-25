using System;
using System.Collections.Generic;
using System.Linq;

namespace Vascular.Geometry.Graphs
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Identify all endpoints of protrusions that are not granted immunity by the specified
        /// predicate <paramref name="immune"/>, and remove them from the graph. Specify null for
        /// the default behaviour of every node being a candidate for removal.
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <param name="graph"></param>
        /// <param name="immune"></param>
        public static void RemoveLeafBranches<TV, TE>(this Graph<TV, TE> graph, Func<TV, bool> immune = null)
            where TV : Vertex<TV, TE>, new()
            where TE : Edge<TV, TE>, new()
        {
            immune ??= v => false;
            var R = graph.V.Values
                .Where(v => v.E.Count == 1 && !immune(v))
                .ToList();
            foreach (var r in R)
            {
                // Has it been removed already? Can happen when we have a single loose branch
                if (!graph.V.ContainsKey(r.P))
                {
                    continue;
                }

                var v = r;
                while (true)
                {
                    // Edge case, the node has no edges
                    if (v.E.Count == 0)
                    {
                        graph.V.Remove(v.P);
                        break;
                    }
                    // Get the opposite end of the attached edge (know that current vertex has 1 branch)
                    // Remove the current vertex, try moving to the next one
                    var e = v.E.First.Value;
                    var n = e.Other(v);
                    graph.RemoveVertex(v);
                    if (n.E.Count > 1)
                    {
                        break;
                    }
                    v = n;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static IEnumerable<(TV s, TV e)> LeafBranches<TV, TE>(this Graph<TV, TE> graph)
            where TV : Vertex<TV, TE>, new()
            where TE : Edge<TV, TE>, new()
        {
            var single = graph.V.Values
                .Where(v => v.E.Count == 1)
                .ToList();
            var visited = new HashSet<TV>(single.Count);
            foreach (var s in single)
            {
                if (visited.Contains(s))
                {
                    continue;
                }
                var E = s.E.First.Value;
                var e = WalkBranch(s, E);
                yield return (s, e);
                visited.Add(s);
                visited.Add(e);
            }
        }

        /// <summary>
        /// Given a node, walk in both directions until the endpoints are hit.
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <param name="v"></param>
        /// <returns></returns>
        public static (TV v0, TV v1) WalkBranch<TV, TE>(this TV v)
            where TV : Vertex<TV, TE>
            where TE : Edge<TV, TE>
        {
            var e0 = v.E.First.Value;
            var e1 = v.E.Last.Value;
            return (WalkBranch(v, e0), WalkBranch(v, e1));
        }

        /// <summary>
        /// Given a starting node <paramref name="v"/> and an attached edge <paramref name="e"/>,
        /// walk the branch until a node is encountered which does not have 2 edges.
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <param name="v"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static TV WalkBranch<TV, TE>(TV v, TE e)
            where TV : Vertex<TV, TE>
            where TE : Edge<TV, TE>
        {
            while (true)
            {
                var ve = e.Other(v);
                if (ve.E.Count != 2)
                {
                    return ve;
                }
                var e0 = ve.E.First.Value;
                var e1 = ve.E.Last.Value;
                e = e == e0 ? e1 : e0;
            }
        }

        /// <summary>
        /// Given a starting node <paramref name="v"/> in a graph <paramref name="G"/>,
        /// iterate all connected nodes and return the edges visited along the way.
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <param name="G"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static (HashSet<TE> e, HashSet<TV> v) ConnectedComponent<TV, TE>(this Graph<TV, TE> G, TV v)
            where TV : Vertex<TV, TE>, new()
            where TE : Edge<TV, TE>, new()
        {
            var S = new Stack<TV>();
            S.Push(v);

            var E = new HashSet<TE>(G.E.Count, G.E.Comparer);
            var V = new HashSet<TV>(G.V.Count) { v };

            while (S.Count > 0)
            {
                v = S.Pop();
                foreach (var e in v.E)
                {
                    if (!E.Contains(e))
                    {
                        E.Add(e);
                        var o = e.Other(v);
                        if (!V.Contains(o))
                        {
                            S.Push(o);
                            V.Add(o);
                        }
                    }
                }
            }

            return (E, V);
        }
    }
}
