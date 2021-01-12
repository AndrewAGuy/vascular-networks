using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Lattices.Manipulation;

namespace Vascular.Construction.LSC.Predicates
{
    public static class Extensions
    {
        public static InitialTerminalPredicate AsInitialTerminalPredicate(this ExteriorPredicate e, ClosestBasisFunction f)
        {
            return (S, T) => e(f(T), T);
        }
    }
}
