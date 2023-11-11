using System;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Geometry.Surfaces
{
    /// <summary>
    /// Fast testing intersections with segments.
    /// </summary>
    public class SegmentSurfaceTest : IAxialBoundable
    {
        private readonly Vector3 start;
        private readonly Vector3 end;
        private readonly Vector3 direction;
        private readonly double radius;
        private readonly double offset;
        private readonly Vector3 multiplier;
        private readonly AxialBounds bounds;

        /// <summary>
        ///
        /// </summary>
        public Segment Segment { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="s"></param>
        public SegmentSurfaceTest(Segment s)
        {
            start = s.Start.Position;
            end = s.End.Position;
            direction = end - start;
            radius = s.Radius;
            var d2 = direction.LengthSquared;
            offset = start * direction / d2;
            multiplier = direction / d2;
            bounds = new AxialBounds(s);
            this.Segment = s;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double DistanceToSurface(Vector3 x)
        {
            var lineFactor = x * multiplier - offset;
            var linePoint = lineFactor >= 1.0 ? end : lineFactor <= 0.0 ? start : start + lineFactor * direction;
            return Vector3.Distance(linePoint, x) - radius;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public (double d, Vector3? n) DistanceAndNormalToSurface(Vector3 x)
        {
            var lineFactor = x * multiplier - offset;
            var linePoint = lineFactor >= 1.0 ? end : lineFactor <= 0.0 ? start : start + lineFactor * direction;
            var outwards = x - linePoint;
            var magnitude = outwards.Length;
            return magnitude == 0.0 ? (-radius, null) : (magnitude - radius, outwards / magnitude);
        }

        /// <inheritdoc/>
        public AxialBounds GetAxialBounds()
        {
            return bounds;
        }

        /// <summary>
        /// Calculates the maximum overlap between segments. Borrows heavily from intersection resolution code.
        /// Must have <paramref name="d"/> = <paramref name="x1"/> - <paramref name="x0"/>, allows for value to
        /// be cached instead of computed each time.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="x1"></param>
        /// <param name="d"></param>
        /// <param name="r"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public double Overlap(Vector3 x0, Vector3 x1, Vector3 d, double r, double s2)
        {
            var nor = multiplier ^ d;
            var nor2 = nor.LengthSquared;
            var sin2 = nor2 / d.LengthSquared;
            if (sin2 <= s2)
            {
                // Indefinite case
                var (S, E) = LinearAlgebra.LineFactors(start, direction, x0, x1);
                var (s, e) = LinearAlgebra.LineFactors(x0, d, start, end);
                var sa = Math.Min(S, E);
                var ea = Math.Max(S, E);
                var sb = Math.Min(s, e);
                var eb = Math.Max(s, e);
                var rT = radius + r;

                if (sa <= 1 && ea >= 0 && sb <= 1 && eb >= 0)
                {
                    // We have a valid intersection range, separation along this range is found from perpendicular distance
                    var aToBS = x0 - (start + S * direction);
                    var sep2 = aToBS.LengthSquared;
                    return rT - Math.Sqrt(sep2);
                }
                else
                {
                    // No intersection range, test the endpoints to get the closest points
                    var d2m = Math.Min(Vector3.DistanceSquared(start, x0), Vector3.DistanceSquared(start, x1));
                    var D2M = Math.Min(Vector3.DistanceSquared(end, x0), Vector3.DistanceSquared(end, x1));
                    var d2 = Math.Min(d2m, D2M);
                    return rT - Math.Sqrt(d2);
                }
            }
            else
            {
                // Definite case
                nor /= Math.Sqrt(nor2);
                var sol = LinearAlgebra.SolveMatrix3x3(direction, -d, nor, x0 - start);
                double f0, f1;
                // As in intersection testing, if one segment factor is outside of the range (0, 1), clamp and project onto other.
                if (sol.x > 1)
                {
                    (f0, f1) = (1, sol.y > 1 ? 1 : sol.y < 0 ? 0 : LinearAlgebra.LineFactor(x0, d, end).Clamp(0, 1));
                }
                else if (sol.x < 0)
                {
                    (f0, f1) = (0, sol.y > 1 ? 1 : sol.y < 0 ? 0 : LinearAlgebra.LineFactor(x0, d, start).Clamp(0, 1));
                }
                else
                {
                    if (sol.y > 1)
                    {
                        (f0, f1) = (LinearAlgebra.LineFactor(start, direction, x1).Clamp(0, 1), 1);
                    }
                    else if (sol.y < 0)
                    {
                        (f0, f1) = (LinearAlgebra.LineFactor(start, direction, x0).Clamp(0, 1), 0);
                    }
                    else
                    {
                        (f0, f1) = (sol.x, sol.y);
                    }
                }
                var c = start + direction * f0;
                var C = x0 + d * f1;
                var sep = Vector3.Distance(C, c);
                return r + radius - sep;
            }
        }

        /// <summary>
        /// See <see cref="Overlap(Vector3, Vector3, Vector3, double, double)"/>.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public double Overlap(SegmentSurfaceTest other, double s2)
        {
            return Overlap(other.start, other.end, other.direction, other.radius, s2);
        }
    }
}
