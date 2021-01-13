using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;

namespace Vascular.Construction.LSC.Defaults
{
    using Surface = IAxialBoundsQueryable<TriangleSurfaceTest>;

    public static class MeshExterior
    {
        public static TerminalPairPredicate TerminalPairNotPenetrating(Surface surface, double rayTolerance)
        {
            return (T, t) => surface.RayIntersectionCount(T.Position, t.Position - T.Position, rayTolerance) == 0;
        }

        public static TerminalPairPredicate TerminalPairNotLeaving(Surface surface, double rayTolerance)
        {
            return (T, t) => surface.RayIntersectionCounts(T.Position, t.Position - T.Position, rayTolerance).outwards == 0;
        }

        public static TerminalPairPredicate TerminalContained(Surface surface, Vector3[] testDirections, int minHits)
        {
            return (T, t) => surface.IsPointInside(t.Position, testDirections, minHits);
        }

        public static InitialTerminalPredicate InitialPairNotPenetrating(Surface surface, double rayTolerance)
        {
            return (S, T) => surface.RayIntersectionCount(S, T - S, rayTolerance) == 0;
        }

        public static InitialTerminalPredicate InitialPairPenetrating(Surface surface, double rayTolerance)
        {
            return (S, T) =>
            {
                (var i, var o) = surface.RayIntersectionCounts(S, T - S, rayTolerance);
                return i != 0 && o == 0;
            };
        }
    }
}
