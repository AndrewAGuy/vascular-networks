using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Geometry.Lattices.Manipulation;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC.Defaults
{
    /// <summary>
    /// A set of delegates that ensure that in each iteration, only one bifurcation happens from each terminal.
    /// Compensates for this by readding candidates, so more iterations are taken overall.
    /// </summary>
    public class SingleBuild
    {
        /// <summary>
        /// 
        /// </summary>
        public Action BeforeSpread { get; }

        /// <summary>
        /// 
        /// </summary>
        public TerminalPairPredicate Predicate { get; }

        /// <summary>
        /// 
        /// </summary>
        public TerminalPairBuildAction OnBuild { get; }

        /// <summary>
        /// 
        /// </summary>
        public Action AfterSpread { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="toIntegral"></param>
        public SingleBuild(LatticeState state, ClosestBasisFunction toIntegral = null)
        {
            toIntegral ??= state.ClosestBasisFunction;
            var built = new HashSet<Terminal>();
            var readding = new HashSet<Vector3>();
            this.BeforeSpread = () =>
            {
                built.Clear();
                readding.Clear();
            };
            this.Predicate = (T, t) =>
            {
                if (built.Contains(T))
                {
                    readding.Add(toIntegral(t.Position));
                    return false;
                }
                return true;
            };
            this.OnBuild = (T, t) =>
            {
                built.Add(T);
                readding.Remove(toIntegral(t.Position));
            };
            this.AfterSpread = () =>
            {
                foreach (var e in readding)
                {
                    state.AddExterior(e);
                }
            };
        }
    }
}
