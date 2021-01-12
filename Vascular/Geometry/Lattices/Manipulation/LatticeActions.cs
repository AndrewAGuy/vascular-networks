using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Geometry.Lattices.Manipulation
{
    using SingleMap = Dictionary<Vector3, Terminal>;
    using MultipleMap = Dictionary<Vector3, ICollection<Terminal>>;

    public delegate Vector3 ClosestBasisFunction(Vector3 v);

    public static class LatticeActions
    {
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

        public static void AddExterior<TCollection>(MultipleMap exterior, SingleMap interior,
            IEnumerable<Vector3> adding, Vector3[] connections)
            where TCollection : ICollection<Terminal>, new()
        {
            foreach (var add in adding)
            {
                AddExterior<TCollection>(exterior, interior, add, connections);
            }
        }

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

        public static void AddExterior<TCollection>(MultipleMap exterior, MultipleMap interior,
            IEnumerable<Vector3> adding, Vector3[] connections)
            where TCollection : ICollection<Terminal>, new()
        {
            foreach (var add in adding)
            {
                AddExterior<TCollection>(exterior, interior, add, connections);
            }
        }

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

        public static SingleMap GetSingleInterior(Branch root, Lattice lattice)
        {
            var interior = new SingleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior[lattice.ClosestVectorBasis(t.Position)] = t);
            return interior;
        }

        public static SingleMap GetSingleInterior(Branch root, VoronoiCellWalker walker)
        {
            var interior = new SingleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior[walker.NearestBasis(t.Position)] = t);
            return interior;
        }

        public static SingleMap GetSingleInterior(Branch root, ClosestBasisFunction basis)
        {
            var interior = new SingleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior[basis(t.Position)] = t);
            return interior;
        }

        public static MultipleMap GetMultipleInterior<TCollection>(Branch root, Lattice lattice)
            where TCollection : ICollection<Terminal>, new()
        {
            var interior = new MultipleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior.ExistingOrNew(
                lattice.ClosestVectorBasis(t.Position), () => new TCollection()).Add(t));
            return interior;
        }

        public static MultipleMap GetMultipleInterior<TCollection>(Branch root, VoronoiCellWalker walker)
            where TCollection : ICollection<Terminal>, new()
        {
            var interior = new MultipleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior.ExistingOrNew(
                walker.NearestBasis(t.Position), () => new TCollection()).Add(t));
            return interior;
        }

        public static MultipleMap GetMultipleInterior<TCollection>(Branch root, ClosestBasisFunction basis)
            where TCollection : ICollection<Terminal>, new()
        {
            var interior = new MultipleMap(Terminal.CountDownstream(root));
            Terminal.ForDownstream(root, t => interior.ExistingOrNew(
                basis(t.Position), () => new TCollection()).Add(t));
            return interior;
        }

        public static SingleMap Reduce(MultipleMap map, Func<Vector3, ICollection<Terminal>, Terminal> selector)
        {
            var reduced = new SingleMap(map.Count);
            foreach (var kv in map)
            {
                reduced[kv.Key] = selector(kv.Key, kv.Value);
            }
            return reduced;
        }

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

        public static void MatchTerminals(params SingleMap[] interiors)
        {
            foreach (var i in interiors[0])
            {
                var T = new Terminal[interiors.Length];
                T[0] = i.Value;

                for (var n = 1; n < interiors.Length; ++n)
                {
                    if (!interiors[n].TryGetValue(i.Key, out T[n]))
                    {
                        goto FAIL;
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

        public static void MatchTerminals(Lattice lattice, params Network[] networks)
        {
            var interiors = networks.Select(network => GetSingleInterior(network.Root, lattice)).ToArray();
            MatchTerminals(interiors);
            foreach (var network in networks)
            {
                network.Partners = networks;
            }
        }

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

        public static ClosestBasisFunction LatticeClosest(Lattice lattice)
        {
            return v => lattice.ClosestVectorBasis(v);
        }

        public static ClosestBasisFunction WalkerClosest(VoronoiCellWalker walker)
        {
            return v => walker.NearestBasis(v);
        }
    }
}
