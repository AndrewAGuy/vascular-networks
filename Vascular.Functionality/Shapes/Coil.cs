using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Functionality.Shapes
{
    public static class Coil
    {
        public static List<Segment> Wrap(IReadOnlyList<Segment> line, double offset, double rate,
            Vector3 offsetDirection, double coilRadius, double deltaAngle)
        {
            var segs = new List<Segment>[line.Count];
            segs[0] = Helix(line[0].Start.Position, line[0].End.Position, offset, rate, offsetDirection, deltaAngle, coilRadius);
            for (var i = 1; i < line.Count; ++i)
            {
                offsetDirection = segs[i - 1][^1].End.Position - line[i].Start.Position;
                segs[i] = Helix(line[i].Start.Position, line[i].End.Position, offset, rate, offsetDirection, deltaAngle, coilRadius);
            }
            var coil = new List<Segment>(segs.Sum(s => s.Count) + line.Count);
            coil.AddRange(segs[0]);
            for (var i = 1; i < line.Count; ++i)
            {
                coil.Add(new()
                {
                    Start = segs[i - 1][^1].End,
                    End = segs[i][0].Start,
                    Radius = coilRadius
                });
                coil.AddRange(segs[i]);
            }
            return coil;
        }

        public static List<Segment> Helix(Vector3 segmentStart, Vector3 segmentEnd, double offset, double rate,
            Vector3 offsetDirection, double deltaAngle, double coilRadius)
        {
            var segmentDirection = segmentEnd - segmentStart;
            var segmentLength = segmentDirection.Length;
            segmentDirection /= segmentLength;
            offsetDirection = LinearAlgebra.RemoveComponent(offsetDirection, segmentDirection).Normalize();
            var start = new Dummy() { Position = segmentStart + offsetDirection * offset };
            var totalAngle = rate * segmentLength;
            var steps = Math.Ceiling(Math.Abs(totalAngle / deltaAngle));
            deltaAngle = totalAngle / steps;
            var rotationMatrix = Matrix3.AxisAngleRotation(segmentDirection, deltaAngle);
            var segments = new List<Segment>((int)steps);
            for (var i = 1; i <= steps; ++i)
            {
                offsetDirection = (rotationMatrix * offsetDirection).Normalize();
                var length = segmentLength * (i / steps);
                var end = new Dummy() { Position = segmentStart + length * segmentDirection + offsetDirection * offset };
                segments.Add(new()
                {
                    Start = start,
                    End = end,
                    Radius = coilRadius
                });
                start = end;
            }
            return segments;
        }
    }
}
