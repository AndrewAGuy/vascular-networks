using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    using Surface = IAxialBoundsQueryable<TriangleSurfaceTest>;

    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="branch"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static IEnumerable<TriangleIntersection> IntersectionSequence(this Surface surface, Branch branch, bool radius = true)
        {
            foreach (var s in branch.Segments)
            {
                var I = surface.RayIntersections(s.Start.Position, s.Direction, radius ? s.Radius : 0);
                foreach (var i in I.OrderBy(ri => ri.Fraction))
                {
                    yield return new(s, i.Object, i.Fraction);
                }
            }
        }
    }
}
