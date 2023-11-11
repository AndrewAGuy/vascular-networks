using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Lattices;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSV.Defaults
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
                    foreach (var ut in element.SingleInterior!)
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
        /// Uses the bases of successive pairs of lattices to determine the downwards number of generations,
        /// i.e., the number of generations that will be executed before coarsening when arriving from the
        /// more coarse lattice. Defaults to using determinant ratios.
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="iterations"></param>
        public static void SetGenerationsByBasis(IEnumerable<LatticeState> elements, Func<Matrix3, Matrix3, int>? iterations = null)
        {
            iterations ??= GenerationsFromDeterminant();
            var e = elements.GetEnumerator();
            if (!e.MoveNext())
            {
                return;
            }

            var previous = e.Current;
            while (e.MoveNext())
            {
                e.Current.GenerationsDown = iterations(previous.Lattice.Basis, e.Current.Lattice.Basis);
                previous = e.Current;
            }
        }

        /// <summary>
        /// Assumes that characteristic length and hence iterations scales as determinant, useful when lattices
        /// are of the same form but with different scales.
        /// </summary>
        /// <returns></returns>
        public static Func<Matrix3, Matrix3, int> GenerationsFromDeterminant()
        {
            return (A, B) => (int)Math.Ceiling(Math.Pow(A.Determinant / B.Determinant, 1.0 / 3.0));
        }

        /// <summary>
        /// Uses induced Lp norms for lattice bases as proxy for the characteristic length, then uses this ratio
        /// to determine how many iterations are required.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Func<Matrix3, Matrix3, int> GenerationsFromLpNorm(double p = double.PositiveInfinity)
        {
            return (A, B) =>
            {
                var r = p switch
                {
                    1 => A.NormL1 / B.NormL1,
                    2 => A.NormL2 / B.NormL2,
                    double.PositiveInfinity => A.NormLInf / B.NormLInf,
                    _ => throw new GeometryException("Induced Lp norms outside 1, 2 and Inf are not supported.")
                };
                return (int)Math.Ceiling(r);
            };
        }

        /// <summary>
        /// Sets the flow rate for new terminals, and sets an interior filter which uniformly sets the flow rate
        /// of each existing terminal such that the total flow associated with the lattice point is equal to this.
        /// Does not account for proximity between existing terminals.
        /// </summary>
        /// <param name="Q0"></param>
        /// <param name="ls"></param>
        /// <returns></returns>
        public static void SetFlowByDeterminant(double Q0, LatticeState ls)
        {
            var Q = Q0 * ls.Lattice.Determinant;
            ls.TerminalFlowFunction = (z, x) => Q;
            ls.InteriorFilter = (z, x, T) =>
            {
                var q = Q / T.Count;
                foreach (var t in T)
                {
                    t.SetFlow(q);
                }
            };
        }

        /// <summary>
        /// Sets flow rate of terminals in the interior filter such that the total flow between all terminals
        /// associated with a lattice point is equal to the rate given by <see cref="LatticeState.TerminalFlowFunction"/>,
        /// with flow being uniformly distributed across the terminals.
        /// Similar to <see cref="SetFlowByDeterminant(double, LatticeState)"/>, except that the terminal flow is not
        /// uniform and must be specified separately.
        /// </summary>
        /// <param name="ls"></param>
        public static void AverageFlowFilter(LatticeState ls)
        {
            ls.InteriorFilter = (z, x, T) =>
            {
                var q = ls.TerminalFlowFunction(z, x) / T.Count;
                foreach (var t in T)
                {
                    t.SetFlow(q);
                }
            };
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
        /// <param name="includeCentre"></param>
        /// <returns></returns>
        public static InteriorFilter KeepClosestToConnection(Lattice lattice, bool includeCentre = false)
        {
            var C = lattice.VoronoiCell.Connections;
            if (includeCentre)
            {
                C = C.Append(Vector3.ZERO).ToArray();
            }
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
