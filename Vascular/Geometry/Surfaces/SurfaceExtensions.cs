using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Triangulation;

namespace Vascular.Geometry.Surfaces
{
    /// <summary>
    /// Fraction along the ray where the object is hit.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public record struct RayIntersection<T>(T Object, double Fraction);

    /// <summary>
    ///
    /// </summary>
    public static class SurfaceExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static double SquaredDistanceToSurface(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, double radius)
        {
            var dist2 = double.PositiveInfinity;
            var bounds = new AxialBounds(point, radius);
            surface.Query(bounds, triangle =>
            {
                var d2 = triangle.DistanceSquared(point);
                dist2 = Math.Min(d2, dist2);
            });
            return dist2;
        }

        /// <summary>
        /// If <paramref name="mode"/> is positive, records outwards intersections, if negative records inwards,
        /// if zero records all.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="radius"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static double RayIntersection(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3 direction, double radius = 0, int mode = 0)
        {
            var minFraction = double.PositiveInfinity;
            Vector3? hitPoint = null;
            var hitFraction = 0.0;
            var bounds = new AxialBounds(point, point + direction, radius);
            surface.Query(bounds, triangle =>
            {
                if (triangle.Normal * direction * mode >= 0 &&
                    triangle.TestRay(point, direction, radius, ref hitFraction, ref hitPoint))
                {
                    minFraction = Math.Min(minFraction, hitFraction);
                }
            });
            return minFraction;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static bool RayIntersects(
            this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3 direction, double radius)
        {
            var bounds = new AxialBounds(point, point + direction, radius);
            var fraction = 0.0;
            Vector3? hitPoint = null;
            bool test(TriangleSurfaceTest triangle)
                => triangle.TestRay(point, direction, radius, ref fraction, ref hitPoint);
            return surface.Query(bounds, test);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="rayTolerance"></param>
        /// <returns></returns>
        public static List<RayIntersection<TriangleSurfaceTest>> RayIntersections(
            this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3 direction, double rayTolerance)
        {
            var intersections = new List<RayIntersection<TriangleSurfaceTest>>();
            var bounds = new AxialBounds(point, point + direction, rayTolerance);
            surface.Query(bounds, triangle =>
            {
                var fraction = 0.0;
                Vector3? hitPoint = null;
                if (triangle.TestRay(point, direction, rayTolerance, ref fraction, ref hitPoint))
                {
                    intersections.Add(new RayIntersection<TriangleSurfaceTest>(triangle, fraction));
                }
            });
            return intersections;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="rayTolerance"></param>
        /// <returns></returns>
        public static int RayIntersectionCount(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3 direction, double rayTolerance)
        {
            var intersections = 0;
            var bounds = new AxialBounds(point, point + direction, rayTolerance);
            surface.Query(bounds, triangle =>
            {
                var fraction = 0.0;
                Vector3? hitPoint = null;
                if (triangle.TestRay(point, direction, rayTolerance, ref fraction, ref hitPoint))
                {
                    ++intersections;
                }
            });
            return intersections;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="rayTolerance"></param>
        /// <returns></returns>
        public static (int inwards, int outwards) RayIntersectionCounts(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3 direction, double rayTolerance)
        {
            var bounds = new AxialBounds(point, point + direction, rayTolerance);
            var inwards = 0;
            var outwards = 0;
            surface.Query(bounds, triangle =>
            {
                var fraction = 0.0;
                Vector3? hitPoint = null;
                if (triangle.TestRay(point, direction, rayTolerance, ref fraction, ref hitPoint))
                {
                    if (triangle.Normal * direction > 0)
                    {
                        ++outwards;
                    }
                    else
                    {
                        ++inwards;
                    }
                }
            });
            return (inwards, outwards);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="testDirection"></param>
        /// <param name="rayTolerance"></param>
        /// <param name="fractionRounding"></param>
        /// <returns></returns>
        public static bool IsPointInside(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3 testDirection, double rayTolerance, int fractionRounding)
        {
            var hits = new HashSet<double>();
            var rayBounds = new AxialBounds(point).Append(point + testDirection).Extend(rayTolerance);
            surface.Query(rayBounds, t =>
            {
                double hitFraction = 0;
                Vector3? hitPoint = null;
                if (t.TestRay(point, testDirection, rayTolerance, ref hitFraction, ref hitPoint))
                {
                    hits.Add(Math.Round(hitFraction, fractionRounding, MidpointRounding.ToPositiveInfinity));
                }
            });
            return hits.Count % 2 != 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="testDirections"></param>
        /// <param name="minHits"></param>
        /// <param name="rayTolerance"></param>
        /// <param name="fractionRounding"></param>
        /// <returns></returns>
        public static bool IsPointInside(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3[] testDirections, int minHits, double rayTolerance, int fractionRounding)
        {
            var hits = 0;
            foreach (var testDirection in testDirections)
            {
                if (surface.IsPointInside(point, testDirection, rayTolerance, fractionRounding))
                {
                    hits++;
                }
            }
            return hits >= minHits;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="testDirection"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool IsPointInside(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3 testDirection, double tol)
        {
            var count = 0;
            var rayBounds = new AxialBounds(point).Append(point + testDirection).Extend(tol);
            surface.Query(rayBounds, t =>
            {
                double f = 0;
                Vector3? p = null;
                if (t.TestRay(point, testDirection, tol, ref f, ref p))
                {
                    count++;
                }
            });
            return count % 2 != 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="testDirections"></param>
        /// <param name="minHits"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool IsPointInside(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3[] testDirections, int minHits, double tol)
        {
            var hits = 0;
            foreach (var d in testDirections)
            {
                if (surface.IsPointInside(point, d, tol))
                {
                    hits++;
                }
            }
            return hits >= minHits;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="testDirection"></param>
        /// <returns></returns>
        public static bool IsPointInside(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3 testDirection)
        {
            var minFraction = double.PositiveInfinity;
            var outwards = false;
            var rayBounds = new AxialBounds(point).Append(point + testDirection);
            surface.Query(rayBounds, triangle =>
            {
                var hitFraction = 0.0;
                var hitPoint = (Vector3?)null;
                if (triangle.TestRay(point, testDirection, 0.0, ref hitFraction, ref hitPoint))
                {
                    if (hitFraction < minFraction)
                    {
                        minFraction = hitFraction;
                        outwards = triangle.Normal * testDirection > 0;
                    }
                }
            });
            return minFraction <= 1.0 && outwards;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="point"></param>
        /// <param name="testDirections"></param>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public static bool IsPointInside(this IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Vector3 point, Vector3[] testDirections, int minHits)
        {
            var hits = 0;
            foreach (var direction in testDirections)
            {
                if (surface.IsPointInside(point, direction))
                {
                    hits++;
                }
            }
            return hits >= minHits;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static double Volume(this Mesh mesh)
        {
            var total = 0.0;
            var bounds = mesh.GetAxialBounds();
            var centre = (bounds.Lower + bounds.Upper) * 0.5;
            foreach (var triangle in mesh.T)
            {
                var v1 = triangle.A.P - centre;
                var v2 = triangle.B.P - centre;
                var v3 = triangle.C.P - centre;
                total += (v1 ^ v2) * v3;
            }
            return Math.Abs(total) / 6.0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        public static double Volume(this IEnumerable<TriangleSurfaceTest> surface)
        {
            var total = 0.0;
            var bounds = surface.GetTotalBounds();
            var centre = (bounds.Lower + bounds.Upper) * 0.5;
            foreach (var triangle in surface)
            {
                var v1 = triangle.A - centre;
                var v2 = triangle.B - centre;
                var v3 = triangle.C - centre;
                total += (v1 ^ v2) * v3;
            }
            return Math.Abs(total) / 6.0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="surface"></param>
        /// <returns></returns>
        public static double Volume(this IAxialBoundsQueryable<TriangleSurfaceTest> surface)
        {
            var total = 0.0;
            var bounds = surface.GetAxialBounds();
            var centre = (bounds.Lower + bounds.Upper) * 0.5;
            foreach (var triangle in surface)
            {
                var v1 = triangle.A - centre;
                var v2 = triangle.B - centre;
                var v3 = triangle.C - centre;
                total += (v1 ^ v2) * v3;
            }
            return Math.Abs(total) / 6.0;
        }
    }
}
