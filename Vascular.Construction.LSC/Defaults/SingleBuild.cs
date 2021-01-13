using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Lattices.Manipulation;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC.Defaults
{
    public class SingleBuild
    {
        public Action BeforeSpread { get; }
        public TerminalPairPredicate Predicate { get; }
        public TerminalPairBuildAction OnBuild { get; }
        public Action AfterSpread { get; }

        public SingleBuild(LatticeState state, ClosestBasisFunction toIntegral)
        {
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
