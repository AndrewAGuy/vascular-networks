using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;

namespace Vascular.Construction.LSC.Predicates
{
    public static class MeshExterior
    {
        public static TerminalPairPredicate TerminalPairNotPenetrating(IAxialBoundsQueryable<TriangleSurfaceTest> surface, double rayTolerance)
        {
            return (T, t) => surface.RayIntersectionCount(T.Position, t.Position - T.Position, rayTolerance) == 0;
        }
    }
}
