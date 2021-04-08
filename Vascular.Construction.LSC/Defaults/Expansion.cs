using System;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.LSC.Defaults
{
    using Surface = IAxialBoundsQueryable<TriangleSurfaceTest>;

    /// <summary>
    /// Delegates for modelling the growth of organs using a fixed lattice and instead expanding the boundary,
    /// as well as the network itself.
    /// </summary>
    public static class Expansion
    {
        /// <summary>
        /// Maps each node to its new position, optionally recalculates derived physical properties.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="map"></param>
        /// <param name="recalc"></param>
        /// <returns></returns>
        public static Action BeforeRefine(Network network, Func<Vector3, Vector3> map, bool recalc = false)
        {
            return () =>
            {
                foreach (var node in network.Nodes)
                {
                    switch (node)
                    {
                        case Source source:
                            source.SetPosition(map(source.Position));
                            break;
                        case Terminal terminal:
                            terminal.SetPosition(map(terminal.Position));
                            break;
                        case IMobileNode mobileNode:
                            mobileNode.Position = map(mobileNode.Position);
                            break;
                    }
                }

                if (recalc)
                {
                    network.Source.CalculatePhysical();
                }
            };
        }

        /// <summary>
        /// The most simple form of growth modelling, where it expands uniformly in all directions.
        /// </summary>
        /// <param name="centre"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static Func<Vector3, Vector3> Homothety(Vector3 centre, double factor)
        {
            return v => centre + (v - centre) * factor;
        }

        /// <summary>
        /// Combine with <see cref="Homothety(Vector3, double)"/> to create a simple spheroid growth model.
        /// </summary>
        /// <param name="centre"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static ExteriorPredicate Sphere(Vector3 centre, double radius)
        {
            var radius2 = Math.Pow(radius, 2);
            return (z, x) => Vector3.DistanceSquared(centre, x) < radius2;
        }

        /// <summary>
        /// A transform from one spherical organoid to another.
        /// </summary>
        /// <param name="oldCentre"></param>
        /// <param name="oldRadius"></param>
        /// <param name="newCentre"></param>
        /// <param name="newRadius"></param>
        /// <returns></returns>
        public static Func<Vector3, Vector3> Homothety(Vector3 oldCentre, double oldRadius, 
            Vector3 newCentre, double newRadius)
        {
            // v = a + f * (u - a)
            // Given that f = R / r, we then match c -> C to find a
            // C = a + f * (c - a) --> a * (1 - f) = C - f * c
            var f = newRadius / oldRadius;
            var c = (newCentre - f * oldCentre) / (1 - f);
            return Homothety(c, f);
        }

        /// <summary>
        /// Prevent expansion from crossing the boundary defined by <paramref name="surface"/>.
        /// Rather than allowing individual radii, treats all nodes as having buffer zone around them of <paramref name="radius"/>.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="inner"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Func<Vector3, Vector3> ClampToBoundary(Surface surface, Func<Vector3, Vector3> inner, double radius)
        {
            return v =>
            {
                var target = inner(v);
                var direction = target - v;
                var intersections = surface.RayIntersections(v, direction, radius);
                return intersections.ArgMin(r => r.Fraction, out var first, out var fraction)
                    ? v + direction * fraction
                    : target;
            };
        }
    }
}
