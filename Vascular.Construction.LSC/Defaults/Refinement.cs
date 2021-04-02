﻿using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}
