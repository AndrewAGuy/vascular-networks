using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Lattices;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC.Defaults
{
    /// <summary>
    /// Delegates used on moving to a more fine lattice.
    /// </summary>
    public static class Refinement
    {
        /// <summary>
        /// Snap positions and set flows to targets.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static Action SetSingleTerminalPositionAndFlow(LatticeState element)
        {
            return () =>
            {
                if (element.MultipleInterior != null)
                {
                    foreach (var uT in element.MultipleInterior)
                    {
                        var z = uT.Key;
                        var T = uT.Value;
                        if (T.Count == 1)
                        {
                            var x = element.Lattice.ToSpace(z);
                            var Q = element.TerminalFlowFunction(z, x);
                            var t = T.First();
                            t.SetPosition(x);
                            t.SetFlow(Q);
                        }
                    }
                }
                else
                {
                    foreach (var ut in element.SingleInterior)
                    {
                        var z = ut.Key;
                        var t = ut.Value;
                        var x = element.Lattice.ToSpace(z);
                        t.SetPosition(x);
                        t.SetFlow(element.TerminalFlowFunction(z, x));
                    }
                }
            };
        }

        /// <summary>
        /// Flow rate is fixed per unit volume, so per-terminal is dependent on the lattice determinant.
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="baseFlow"></param>
        /// <param name="baseGenerations"></param>
        public static void DeterminantRatioFlowAndGenerations(IEnumerable<LatticeState> elements,
            double baseFlow = 1.0, int baseGenerations = 1)
        {
            var e = elements.GetEnumerator();
            if (!e.MoveNext())
            {
                return;
            }
            var initial = e.Current;
            initial.GenerationsUp = 0;
            initial.TerminalFlowFunction = (z, x) => baseFlow;

            var previous = initial;
            while (e.MoveNext())
            {
                var current = e.Current;
                var baseDeterminantRatio = current.Lattice.Determinant / initial.Lattice.Determinant;
                var flow = baseFlow * baseDeterminantRatio;
                current.TerminalFlowFunction = (z, x) => flow;

                var previousDeterminantRatio = previous.Lattice.Determinant / current.Lattice.Determinant;
                var iterations = (int)Math.Ceiling(Math.Pow(previousDeterminantRatio, 1.0 / 3.0)) + baseGenerations;
                current.GenerationsDown = iterations;
                current.GenerationsUp = 1;

                previous = current;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lattice"></param>
        /// <returns></returns>
        public static InteriorFilter KeepExtremaByConnection(Lattice lattice)
        {
            var C = lattice.VoronoiCell.Connections;
            var d = new Vector3[C.Length];
            var mV = new double[C.Length];
            var mT = new Terminal[C.Length];
            var h = new HashSet<Terminal>(C.Length);

            return (z, x, T) =>
            {
                for (var i = 0; i < C.Length; ++i)
                {
                    d[i] = lattice.ToSpace(z + C[i]) - x;
                }
                mV.SetArray(double.NegativeInfinity);
                mT.SetArray(null);

                foreach (var t in T)
                {
                    var r = t.Position - x;
                    for (var i = 0; i < C.Length; ++i)
                    {
                        var v = r * d[i];
                        if (v > mV[i])
                        {
                            mV[i] = v;
                            mT[i] = t;
                        }
                    }
                }

                T.Clear();
                h.Clear();
                foreach (var t in mT)
                {
                    h.Add(t);
                }
                foreach (var t in h)
                {
                    T.Add(t);
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lattice"></param>
        /// <returns></returns>
        public static InteriorFilter KeepClosestToConnection(Lattice lattice)
        {
            var C = lattice.VoronoiCell.Connections;
            var p = new Vector3[C.Length];
            var mV = new double[C.Length];
            var mT = new Terminal[C.Length];
            var h = new HashSet<Terminal>(C.Length);

            return (z, x, T) =>
            {
                for (var i = 0; i < C.Length; ++i)
                {
                    p[i] = lattice.ToSpace(z + C[i]);
                }
                mV.SetArray(double.PositiveInfinity);
                mT.SetArray(null);

                foreach (var t in T)
                {
                    for (var i = 0; i < C.Length; ++i)
                    {
                        var v = Vector3.DistanceSquared(t.Position, p[i]);
                        if (v < mV[i])
                        {
                            mV[i] = v;
                            mT[i] = t;
                        }
                    }
                }

                T.Clear();
                h.Clear();
                foreach (var t in mT)
                {
                    h.Add(t);
                }
                foreach (var t in h)
                {
                    T.Add(t);
                }
            };
        }
    }
}
