using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Geometry.Lattices.Manipulation
{
    using SingleMap = Dictionary<Vector3, Terminal>;
    using MultipleMap = Dictionary<Vector3, ICollection<Terminal>>;

    /// <summary>
    ///
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public delegate Vector3 ClosestBasisFunction(Vector3 v);

    /// <summary>
    /// Used heavily by Lattice Sequence Construction.
    /// </summary>
    public static class LatticeActions
    {
        /// <summary>
        /// Given an interior map and the connection pattern, find all vectors such that
        /// e = i + c with i in <paramref name="interior"/> and c in <paramref name="connections"/>,
        /// such that v is not in <paramref name="interior"/>. For each of these vectors e, store all
        /// vectors i such that e = i + c.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="interior"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public static MultipleMap GetExterior<TCollection>(
            SingleMap interior, Vector3[] connections)
            where TCollection : ICollection<Terminal>, new()
        {
            var exterior = new MultipleMap(interior.Count * connections.Length);
            foreach (var u in interior.Keys)
            {
                foreach (var c in connections)
                {
                    var v = u + c;
                    if (!interior.ContainsKey(v) && !exterior.ContainsKey(v))
                    {
                        exterior[v] = GetConnected<TCollection>(interior, connections, v);
                    }
                }
            }
            return exterior;
        }

        /// <summary>
        /// Given an interior, connection pattern and a vector that is exterior,
        /// get the vectors i such that i = e + c.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="interior"></param>
        /// <param name="connections"></param>
        /// <param name="exterior"></param>
        /// <returns></returns>
        public static TCollection GetConnected<TCollection>(
            SingleMap interior, Vector3[] connections, Vector3 exterior)
            where TCollection : ICollection<Terminal>, new()
        {
            var connected = new TCollection();
            foreach (var c in connections)
            {
                var v = exterior + c;
                if (interior.TryGetValue(v, out var t))
                {
                    connected.Add(t);
                }
            }
            return connected;
        }

        /// <summary>
        /// Given an old exterior and a new interior, any vectors in the old exterior that have been added to
        /// the interior are candidates for propagation. Any vectors that are connected to these and exterior
        /// to the new interior are returned, along with their candidate connections. The old exterior, new
        /// interior vectors will always be present in this.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="interior"></param>
        /// <param name="connections"></param>
        /// <param name="oldExterior"></param>
        /// <returns></returns>
        public static MultipleMap PropagateExterior<TCollection>(
            SingleMap interior, Vector3[] connections, ICollection<Vector3> oldExterior)
            where TCollection : ICollection<Terminal>, new()
        {
            var newExterior = new MultipleMap(oldExterior.Count * connections.Length);
            foreach (var u in oldExterior)
            {
                if (interior.ContainsKey(u))
                {
                    foreach (var c in connections)
                    {
                        var v = u + c;
                        if (!interior.ContainsKey(v) && !newExterior.ContainsKey(v))
                        {
                            newExterior[v] = GetConnected<TCollection>(interior, connections, v);
                        }
                    }
                }
            }
            return newExterior;
        }

        /// <summary>
        /// Wrapper around <see cref="AddExterior{TCollection}(MultipleMap, SingleMap, Vector3, Vector3[])"/>
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="exterior"></param>
        /// <param name="interior"></param>
        /// <param name="adding"></param>
        /// <param name="connections"></param>
        public static void AddExterior<TCollection>(MultipleMap exterior, SingleMap interior,
            IEnumerable<Vector3> adding, Vector3[] connections)
            where TCollection : ICollection<Terminal>, new()
        {
            foreach (var add in adding)
            {
                AddExterior<TCollection>(exterior, interior, add, connections);
            }
        }

        /// <summary>
        /// If not present in the exterior, try adding. If not present in interior, try getting connected and
        /// add if nonempty. If present, try adding all in connection pattern to exterior.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="exterior"></param>
        /// <param name="interior"></param>
        /// <param name="add"></param>
        /// <param name="connections"></param>
        public static void AddExterior<TCollection>(MultipleMap exterior, SingleMap interior,
            Vector3 add, Vector3[] connections)
            where TCollection : ICollection<Terminal>, new()
        {
            if (!exterior.ContainsKey(add))
            {
                if (!interior.ContainsKey(add))
                {
                    var T = GetConnected<TCollection>(interior, connections, add);
                    if (T.Count > 0)
                    {
                        exterior[add] = T;
                    }
                }
                else
                {
                    foreach (var c in connections)
                    {
                        var u = add + c;
                        if (!interior.ContainsKey(u) && !exterior.ContainsKey(u))
                        {
                            exterior[u] = GetConnected<TCollection>(interior, connections, u);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// See <see cref="GetExterior{TCollection}(SingleMap, Vector3[])"/>, but for multiple maps.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="interior"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public static MultipleMap GetExterior<TCollection>(
            MultipleMap interior, Vector3[] connections)
            where TCollection : ICollection<Terminal>, new()
        {
            var exterior = new MultipleMap(interior.Count * connections.Length);
            foreach (var u in interior.Keys)
            {
                foreach (var c in connections)
                {
                    var e = u + c;
                    if (!interior.ContainsKey(e) && !exterior.ContainsKey(e))
                    {
                        exterior[e] = GetConnected<TCollection>(interior, connections, e);
                    }
                }
            }
            return exterior;
        }

        /// <summary>
        /// See <see cref="GetConnected{TCollection}(SingleMap, Vector3[], Vector3)"/>, but for multiple maps.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="interior"></param>
        /// <param name="connections"></param>
        /// <param name="exterior"></param>
        /// <returns></returns>
        public static TCollection GetConnected<TCollection>(
            MultipleMap interior, Vector3[] connections, Vector3 exterior)
            where TCollection : ICollection<Terminal>, new()
        {
            var connected = new TCollection();
            foreach (var c in connections)
            {
                var u = exterior + c;
                if (interior.TryGetValue(u, out var T))
                {
                    foreach (var t in T)
                    {
                        connected.Add(t);
                    }
                }
            }
            return connected;
        }

        /// <summary>
        /// See <see cref="PropagateExterior{TCollection}(SingleMap, Vector3[], ICollection{Vector3})"/>, but for multiple maps.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="interior"></param>
        /// <param name="connections"></param>
        /// <param name="oldExterior"></param>
        /// <returns></returns>
        public static MultipleMap PropagateExterior<TCollection>(MultipleMap interior, Vector3[] connections,
            ICollection<Vector3> oldExterior)
            where TCollection : ICollection<Terminal>, new()
        {
            var newExterior = new MultipleMap(oldExterior.Count * connections.Length);
            foreach (var u in oldExterior)
            {
                if (interior.ContainsKey(u))
                {
                    foreach (var c in connections)
                    {
                        var v = u + c;
                        if (!interior.ContainsKey(v) && !newExterior.ContainsKey(v))
                        {
                            newExterior[v] = GetConnected<TCollection>(interior, connections, v);
                        }
                    }
                }
            }
            return newExterior;
        }

        /// <summary>
        /// See <see cref="AddExterior{TCollection}(MultipleMap, SingleMap, IEnumerable{Vector3}, Vector3[])"/>, but for multiple maps.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="exterior"></param>
        /// <param name="interior"></param>
        /// <param name="adding"></param>
        /// <param name="connections"></param>
        public static void AddExterior<TCollection>(MultipleMap exterior, MultipleMap interior,
            IEnumerable<Vector3> adding, Vector3[] connections)
            where TCollection : ICollection<Terminal>, new()
        {
            foreach (var add in adding)
            {
                AddExterior<TCollection>(exterior, interior, add, connections);
            }
        }

        /// <summary>
        /// See <see cref="AddExterior{TCollection}(MultipleMap, SingleMap, Vector3, Vector3[])"/>, but for multiple maps.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="exterior"></param>
        /// <param name="interior"></param>
        /// <param name="add"></param>
        /// <param name="connections"></param>
        public static void AddExterior<TCollection>(MultipleMap exterior, MultipleMap interior,
            Vector3 add, Vector3[] connections)
            where TCollection : ICollection<Terminal>, new()
        {
            if (!exterior.ContainsKey(add))
            {
                if (!interior.ContainsKey(add))
                {
                    var T = GetConnected<TCollection>(interior, connections, add);
                    if (T.Count > 0)
                    {
                        exterior[add] = T;
                    }
                }
                else
                {
                    foreach (var c in connections)
                    {
                        var u = add + c;
                        if (!interior.ContainsKey(u) && !exterior.ContainsKey(u))
                        {
                            exterior[u] = GetConnected<TCollection>(interior, connections, u);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a lattice and network, add all terminals to a single map.
        /// Overwrites happen in the order of visiting.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="lattice"></param>
        /// <returns></returns>
        public static SingleMap GetSingleInterior(Branch root, Lattice lattice)
        {
            var interior = new SingleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior[lattice.ClosestVectorBasis(t.Position)] = t);
            return interior;
        }

        /// <summary>
        /// See <see cref="GetSingleInterior(Branch, Lattice)"/>, but with a <see cref="VoronoiCellWalker"/>.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="walker"></param>
        /// <returns></returns>
        public static SingleMap GetSingleInterior(Branch root, VoronoiCellWalker walker)
        {
            var interior = new SingleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior[walker.NearestBasis(t.Position)] = t);
            return interior;
        }

        /// <summary>
        /// See <see cref="GetSingleInterior(Branch, Lattice)"/>, but using a user-specified function.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="basis"></param>
        /// <returns></returns>
        public static SingleMap GetSingleInterior(Branch root, ClosestBasisFunction basis)
        {
            var interior = new SingleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior[basis(t.Position)] = t);
            return interior;
        }

        /// <summary>
        /// Given a lattice and a network, get the collection of terminals closest to each basis vector.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="root"></param>
        /// <param name="lattice"></param>
        /// <returns></returns>
        public static MultipleMap GetMultipleInterior<TCollection>(Branch root, Lattice lattice)
            where TCollection : ICollection<Terminal>, new()
        {
            var interior = new MultipleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior.ExistingOrNew(
                lattice.ClosestVectorBasis(t.Position), () => new TCollection()).Add(t));
            return interior;
        }

        /// <summary>
        /// See <see cref="GetMultipleInterior{TCollection}(Branch, Lattice)"/>, but uses a <see cref="VoronoiCellWalker"/>.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="root"></param>
        /// <param name="walker"></param>
        /// <returns></returns>
        public static MultipleMap GetMultipleInterior<TCollection>(Branch root, VoronoiCellWalker walker)
            where TCollection : ICollection<Terminal>, new()
        {
            var interior = new MultipleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior.ExistingOrNew(
                walker.NearestBasis(t.Position), () => new TCollection()).Add(t));
            return interior;
        }

        /// <summary>
        /// See <see cref="GetMultipleInterior{TCollection}(Branch, Lattice)"/>, but using a user-specified function.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="root"></param>
        /// <param name="basis"></param>
        /// <returns></returns>
        public static MultipleMap GetMultipleInterior<TCollection>(Branch root, ClosestBasisFunction basis)
            where TCollection : ICollection<Terminal>, new()
        {
            var interior = new MultipleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior.ExistingOrNew(
                basis(t.Position), () => new TCollection()).Add(t));
            return interior;
        }

        /// <summary>
        /// Turns a multiple map into a single map using the given rule.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="selector">Maps the basis vector and terminal collection to the most suitable terminal.</param>
        /// <returns></returns>
        public static SingleMap Reduce(MultipleMap map, Func<Vector3, ICollection<Terminal>, Terminal> selector)
        {
            var reduced = new SingleMap(map.Count);
            foreach (var kv in map)
            {
                reduced[kv.Key] = selector(kv.Key, kv.Value);
            }
            return reduced;
        }

        /// <summary>
        /// Turn a single map into a multiple map where each entry has one element.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="map"></param>
        /// <returns></returns>
        public static MultipleMap Expand<TCollection>(SingleMap map)
            where TCollection : ICollection<Terminal>, new()
        {
            var expanded = new MultipleMap(map.Count);
            foreach (var kv in map)
            {
                expanded[kv.Key] = new TCollection() { kv.Value };
            }
            return expanded;
        }

        /// <summary>
        /// Given a collection of single maps, group all where the key is present in all maps.
        /// </summary>
        /// <param name="interiors"></param>
        public static void MatchTerminals(params SingleMap[] interiors)
        {
            foreach (var i in interiors[0])
            {
                var T = new Terminal[interiors.Length];
                T[0] = i.Value;

                for (var n = 1; n < interiors.Length; ++n)
                {
                    if (!interiors[n].TryGetValue(i.Key, out var t))
                    {
                        goto FAIL;
                    }
                    else
                    {
                        T[n] = t;
                    }
                }

                foreach (var t in T)
                {
                    t.Partners = T;
                }

FAIL:
                continue;
            }
        }

        /// <summary>
        /// Generates maps using <see cref="GetSingleInterior(Branch, Lattice)"/>, then matches using
        /// <see cref="MatchTerminals(SingleMap[])"/>.
        /// </summary>
        /// <param name="lattice"></param>
        /// <param name="networks"></param>
        public static void MatchTerminals(Lattice lattice, params Network[] networks)
        {
            var interiors = networks.Select(network => GetSingleInterior(network.Root!, lattice)).ToArray();
            MatchTerminals(interiors);
            foreach (var network in networks)
            {
                network.Partners = networks;
            }
        }

        /// <summary>
        /// Calls <see cref="Reduce(MultipleMap, Func{Vector3, ICollection{Terminal}, Terminal})"/> on the result of
        /// <see cref="GetMultipleInterior{TCollection}(Branch, Lattice)"/> with a function that picks the minimum distance.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="lattice"></param>
        /// <returns></returns>
        public static SingleMap GetClosestInterior(Branch root, Lattice lattice)
        {
            return Reduce(
                GetMultipleInterior<List<Terminal>>(root, lattice),
                (u, T) =>
                {
                    var v = lattice.ToSpace(u);
                    return T.ArgMin(t => Vector3.DistanceSquared(v, t.Position), out var t, out var V)
                        ? t : throw new TopologyException();
                });
        }

        /// <summary>
        /// Given a collection of <paramref name="before"/> and <paramref name="after"/>, fill the colections
        /// <paramref name="gained"/> and <paramref name="lost"/>.
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="gained"></param>
        /// <param name="lost"></param>
        public static void GetDifference<TCollection>(
            ICollection<Vector3> before, ICollection<Vector3> after,
            out TCollection gained, out TCollection lost)
            where TCollection : ICollection<Vector3>, new()
        {
            gained = new TCollection();
            lost = new TCollection();
            foreach (var b in before)
            {
                if (!after.Contains(b))
                {
                    lost.Add(b);
                }
            }
            foreach (var a in after)
            {
                if (!before.Contains(a))
                {
                    gained.Add(a);
                }
            }
        }

        /// <summary>
        /// Removes a key from the interior and any associated connections in the exterior.
        /// </summary>
        /// <param name="interior"></param>
        /// <param name="exterior"></param>
        /// <param name="terminal"></param>
        /// <param name="index"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public static bool Remove(SingleMap interior, MultipleMap exterior,
            Terminal terminal, Vector3 index, Vector3[] connections)
        {
            var result = interior.Remove(index);

            foreach (var c in connections)
            {
                var u = index + c;
                if (exterior.TryGetValue(u, out var terminals))
                {
                    terminals.Remove(terminal);
                    if (terminals.Count == 0)
                    {
                        exterior.Remove(u);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// See <see cref="Remove(SingleMap, MultipleMap, Terminal, Vector3, Vector3[])"/>, but for multiple maps.
        /// </summary>
        /// <param name="interior"></param>
        /// <param name="exterior"></param>
        /// <param name="terminal"></param>
        /// <param name="index"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public static bool Remove(MultipleMap interior, MultipleMap exterior,
            Terminal terminal, Vector3 index, Vector3[] connections)
        {
            var result = false;
            if (interior.TryGetValue(index, out var terminals))
            {
                result = terminals.Remove(terminal);
                if (terminals.Count == 0)
                {
                    interior.Remove(index);
                }
            }

            foreach (var c in connections)
            {
                var u = index + c;
                if (exterior.TryGetValue(u, out terminals))
                {
                    terminals.Remove(terminal);
                    if (terminals.Count == 0)
                    {
                        exterior.Remove(u);
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="lattice"></param>
        /// <returns></returns>
        public static ClosestBasisFunction LatticeClosest(Lattice lattice)
        {
            return v => lattice.ClosestVectorBasis(v);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="walker"></param>
        /// <returns></returns>
        public static ClosestBasisFunction WalkerClosest(VoronoiCellWalker walker)
        {
            return v => walker.NearestBasis(v);
        }

        /// <summary>
        /// For a lattice <paramref name="L"/> with connection pattern <paramref name="C"/>,
        /// start from point <paramref name="x0"/> (in real space) and walk the connection pattern.
        /// Each point is tested (in real space) by <paramref name="xPredicate"/>, and all connected
        /// lattice sites starting from the closest point to <paramref name="x0"/> are returned.
        /// If <paramref name="x0"/> is outside according to <paramref name="xPredicate"/>, returns
        /// empty set.
        /// </summary>
        /// <param name="L"></param>
        /// <param name="C"></param>
        /// <param name="x0"></param>
        /// <param name="xPredicate"></param>
        /// <returns></returns>
        public static HashSet<Vector3> GetComponent(Lattice L, Vector3[] C, Vector3 x0, Func<Vector3, bool> xPredicate)
        {
            var Z = new HashSet<Vector3>();
            var z0 = L.ClosestVectorBasis(x0);
            var E = new HashSet<Vector3>() { z0 };
            while (E.Count != 0)
            {
                var eNew = new HashSet<Vector3>(E.Count * C.Length);
                foreach (var e in E)
                {
                    var x = L.ToSpace(e);
                    if (!Z.Contains(e) && xPredicate(x))
                    {
                        Z.Add(e);
                        foreach (var c in C)
                        {
                            eNew.Add(e + c);
                        }
                    }
                }
                E = eNew;
            }
            return Z;
        }
    }
}
