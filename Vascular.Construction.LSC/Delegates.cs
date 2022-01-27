using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC
{
    using MultipleMapEntry = KeyValuePair<Vector3, ICollection<Terminal>>;

    /// <summary>
    /// Order in which exterior elements will be attempted to be added.
    /// </summary>
    /// <param name="E"></param>
    /// <returns></returns>
    public delegate IEnumerable<MultipleMapEntry> ExteriorOrderingGenerator(IEnumerable<MultipleMapEntry> E);

    /// <summary>
    /// Whether an exterior vector is allowed to be considered.
    /// </summary>
    /// <param name="z"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public delegate bool ExteriorPredicate(Vector3 z, Vector3 x);

    /// <summary>
    /// When a candidate terminal is created, this sets the flow rate.
    /// </summary>
    /// <param name="z"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public delegate double TerminalFlowFunction(Vector3 z, Vector3 x);

    /// <summary>
    /// Allows different types of terminals to be constructed into the network.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="Q"></param>
    /// <returns></returns>
    public delegate Terminal TerminalConstructor(Vector3 x, double Q);

    /// <summary>
    /// Whether a candidate pair is permissible.
    /// </summary>
    /// <param name="T"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate bool TerminalPairPredicate(Terminal T, Terminal t);

    /// <summary>
    /// Ranks candidate pairs. Lower is better.
    /// </summary>
    /// <param name="T"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate double TerminalPairCostFunction(Terminal T, Terminal t);

    /// <summary>
    /// Chooses which segment in the branch to bifurcate from.
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public delegate Segment BifurcationSegmentSelector(Branch b);

    /// <summary>
    /// Places the bifurcation upon creation.
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public delegate Vector3 BifurcationPositionFunction(Bifurcation b);

    /// <summary>
    /// Executed when a terminal pair is accepted, after the network is modified.
    /// </summary>
    /// <param name="T"></param>
    /// <param name="t"></param>
    public delegate void TerminalPairBuildAction(Terminal T, Terminal t);

    /// <summary>
    /// Order in which basis vectors will be tested.
    /// </summary>
    /// <param name="V"></param>
    /// <returns></returns>
    public delegate IEnumerable<Vector3> InitialTerminalOrderingGenerator(IEnumerable<Vector3> V);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="S"></param>
    /// <param name="T"></param>
    /// <returns></returns>
    public delegate bool InitialTerminalPredicate(Vector3 S, Vector3 T);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="S"></param>
    /// <param name="T"></param>
    /// <returns></returns>
    public delegate double InitialTerminalCostFunction(Vector3 S, Vector3 T);

    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Short circuits, so do not rely on side effects.
        /// </summary>
        /// <param name="P"></param>
        /// <returns></returns>
        public static ExteriorPredicate Combine(this ExteriorPredicate[] P)
        {
            return (z, x) =>
            {
                foreach (var p in P)
                {
                    if (!p(z, x))
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        /// <summary>
        /// Short circuits, so do not rely on side effects.
        /// </summary>
        /// <param name="P"></param>
        /// <returns></returns>
        public static TerminalPairPredicate Combine(this TerminalPairPredicate[] P)
        {
            return (T, t) =>
            {
                foreach (var p in P)
                {
                    if (!p(T, t))
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        /// <summary>
        /// Short circuits, so do not rely on side effects.
        /// </summary>
        /// <param name="P"></param>
        /// <returns></returns>
        public static InitialTerminalPredicate Combine(this InitialTerminalPredicate[] P)
        {
            return (S, T) =>
            {
                foreach (var p in P)
                {
                    if (!p(S, T))
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public static TerminalPairBuildAction Combine(this TerminalPairBuildAction[] A)
        {
            return (T, t) =>
            {
                foreach (var a in A)
                {
                    a(T, t);
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public static Action Combine(this Action[] A)
        {
            return () =>
            {
                foreach (var a in A)
                {
                    a();
                }
            };
        }
    }
}
