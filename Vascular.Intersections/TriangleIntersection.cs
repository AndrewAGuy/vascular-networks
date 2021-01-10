using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Intersections
{
    public class TriangleIntersection
    {
        public TriangleIntersection(Segment s, TriangleSurfaceTest t, double f)
        {
            this.Segment = s;
            this.Triangle = t;
            this.Fraction = f;
            this.Outwards = s.Direction * t.Normal > 0;
        }

        public Segment Segment { get; }
        public TriangleSurfaceTest Triangle { get; }
        public double Fraction { get; }
        public bool Outwards { get; }
    }
}
