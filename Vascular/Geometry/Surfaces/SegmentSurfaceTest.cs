using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Geometry.Acceleration
{
    public class SegmentSurfaceTest : IAxialBoundable
    {
        private readonly Vector3 start;
        private readonly Vector3 end;
        private readonly Vector3 direction;
        private readonly double radius;
        private readonly double offset;
        private readonly Vector3 multiplier;
        private readonly AxialBounds bounds;

        public SegmentSurfaceTest(Segment s)
        {
            start = s.Start.Position;
            end = s.End.Position;
            direction = end - start;
            radius = s.Radius;
            var d2 = direction.LengthSquared;
            offset = (start * direction) / d2;
            multiplier = direction / d2;
            bounds = new AxialBounds(s);
        }

        public double DistanceToSurface(Vector3 x)
        {
            var lineFactor = x * multiplier - offset;
            var linePoint = lineFactor >= 1.0 ? end : lineFactor <= 0.0 ? start : start + lineFactor * direction;
            return Vector3.Distance(linePoint, x) - radius;
        }

        public (double d, Vector3 n) DistanceAndNormalToSurface(Vector3 x)
        {
            var lineFactor = x * multiplier - offset;
            var linePoint = lineFactor >= 1.0 ? end : lineFactor <= 0.0 ? start : start + lineFactor * direction;
            var outwards = x - linePoint;
            var magnitude = outwards.Length;
            return magnitude == 0.0 ? (-radius, null) : (magnitude - radius, outwards / magnitude);
        }

        public AxialBounds GetAxialBounds()
        {
            return bounds;
        }
    }
}
