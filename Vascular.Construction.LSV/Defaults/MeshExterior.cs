﻿using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;

namespace Vascular.Construction.LSV.Defaults
{
    using Surface = IAxialBoundsQueryable<TriangleSurfaceTest>;

    /// <summary>
    /// Delegates for working with bounding meshes.
    /// </summary>
    public static class MeshExterior
    {
        /// <summary>
        /// Ensure that the line between terminal pair does not cross the boundary. Does not guarantee that the network will not.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="rayTolerance"></param>
        /// <returns></returns>
        public static TerminalPairPredicate TerminalPairNotPenetrating(Surface surface, double rayTolerance)
        {
            return (T, t) => surface.RayIntersectionCount(T.Position, t.Position - T.Position, rayTolerance) == 0;
        }

        /// <summary>
        /// Allow entries to the mesh.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="rayTolerance"></param>
        /// <returns></returns>
        public static TerminalPairPredicate TerminalPairNotLeaving(Surface surface, double rayTolerance)
        {
            return (T, t) => surface.RayIntersectionCounts(T.Position, t.Position - T.Position, rayTolerance).outwards == 0;
        }

        /// <summary>
        /// Tests whether the bifurcation triad created by a candidate bifurcation could possibly intersect the surface.
        /// Assumes that bifurcations will be placed inside the triad, which is common for all typical costs.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="rayTolerance"></param>
        /// <param name="normalTolerance"></param>
        /// <returns></returns>
        public static TerminalPairPredicate BifurcationTriadNotPenetrating(Surface surface, double rayTolerance, double normalTolerance = 0)
        {
            // TODO: handle case where bifurcation triad becomes degenerate.
            return (T, t) =>
            {
                var tst = new TriangleSurfaceTest(
                    T.Position, t.Position, T.Upstream.Start.Position, rayTolerance, normalTolerance);
                return !surface.Query(tst.GetAxialBounds(), TST => tst.TestTriangleRays(TST, out var a, out var b));
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="testDirections"></param>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public static TerminalPairPredicate TerminalContained(Surface surface, Vector3[] testDirections, int minHits)
        {
            return (T, t) => surface.IsPointInside(t.Position, testDirections, minHits);
        }

        /// <summary>
        /// When the source node is internal.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="rayTolerance"></param>
        /// <returns></returns>
        public static InitialTerminalPredicate InitialPairNotPenetrating(Surface surface, double rayTolerance)
        {
            return (S, T) => surface.RayIntersectionCount(S, T - S, rayTolerance) == 0;
        }

        /// <summary>
        /// When the source node is external.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="rayTolerance"></param>
        /// <returns></returns>
        public static InitialTerminalPredicate InitialPairPenetrating(Surface surface, double rayTolerance)
        {
            return (S, T) =>
            {
                (var i, var o) = surface.RayIntersectionCounts(S, T - S, rayTolerance);
                return i != 0 && o == 0;
            };
        }

        /// <summary>
        /// Convenience method, wraps <see cref="SurfaceExtensions.IsPointInside(Surface, Vector3, Vector3[], int)"/>.
        /// Use when it doesn't matter if it's penetrating or not.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="testDirections"></param>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public static InitialTerminalPredicate InitialTerminalContained(Surface surface, Vector3[] testDirections, int minHits)
        {
            return (S, T) => surface.IsPointInside(T, testDirections, minHits);
        }
    }
}
