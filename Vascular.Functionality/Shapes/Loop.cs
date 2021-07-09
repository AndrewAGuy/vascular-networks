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
    public static class Loop
    {
        public static List<Segment> UnitZX(double length, double turnRadius, int turnSections, double segmentRadius)
        {
            var segs = new List<Segment>(2 + turnSections)
            {
                new()
                {
                    Start = new Dummy() { Position = new(turnRadius, 0, 0) },
                    End = new Dummy() { Position = new(turnRadius, 0, length) },
                    Radius = segmentRadius
                }
            };
            for (var i = 0; i < turnSections; ++i)
            {
                var angle = i * Math.PI / turnSections;
                segs.Add(new()
                {
                    Start = segs[^1].End,
                    End = new Dummy() { Position = new(turnRadius * Math.Cos(angle), 0, length + turnRadius * Math.Sin(angle)) },
                    Radius = segmentRadius
                });
            }
            segs.Add(new()
            {
                Start = segs[^1].End,
                End = new Dummy() { Position = new(-turnRadius, 0, 0) },
                Radius = segmentRadius
            });
            return segs;
        }
    }
}
