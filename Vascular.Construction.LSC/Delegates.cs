using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC
{
    using MultipleMapEntry = KeyValuePair<Vector3, ICollection<Terminal>>;

    public delegate IEnumerable<MultipleMapEntry> ExteriorOrderingGenerator(IEnumerable<MultipleMapEntry> E);
    public delegate bool ExteriorPredicate(Vector3 z, Vector3 x);
    public delegate double TerminalFlowFunction(Vector3 z, Vector3 x);
    public delegate bool TerminalPairPredicate(Terminal T, Terminal t);
    public delegate double TerminalPairCostFunction(Terminal T, Terminal t);
    public delegate Vector3 BifurcationPositionFunction(Bifurcation b);
    public delegate void TerminalPairBuildAction(Terminal T, Terminal t);

    public delegate IEnumerable<Vector3> InitialTerminalOrderingGenerator(IEnumerable<Vector3> V);
    public delegate bool InitialTerminalPredicate(Vector3 S, Vector3 T);
    public delegate double InitialTerminalCostFunction(Vector3 S, Vector3 T);

    public static class Extensions
    {
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
