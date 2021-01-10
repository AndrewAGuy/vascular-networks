using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Intersections
{
    public class MeshIntersectionExtrema
    {
        public MeshIntersectionExtrema(TriangleIntersection ti)
        {
            this.Segment = ti.Segment;
            if (ti.Outwards)
            {
                this.FirstOut = ti.Triangle;
                this.OutFraction = ti.Fraction;
                this.LastIn = null;
                this.InFraction = 0;
            }
            else
            {
                this.LastIn = ti.Triangle;
                this.InFraction = ti.Fraction;
                this.FirstOut = null;
                this.OutFraction = 1;
            }
        }

        public Segment Segment { get; }
        public TriangleSurfaceTest LastIn { get; private set; }
        public double InFraction { get; private set; }
        public TriangleSurfaceTest FirstOut { get; private set; }
        public double OutFraction { get; private set; }

        public void Add(TriangleIntersection ti)
        {
            if (ti.Outwards)
            {
                if (ti.Fraction <= this.OutFraction)
                {
                    this.FirstOut = ti.Triangle;
                    this.OutFraction = ti.Fraction;
                }
            }
            else
            {
                if (ti.Fraction >= this.InFraction)
                {
                    this.LastIn = ti.Triangle;
                    this.InFraction = ti.Fraction;
                }
            }
        }
    }
}
