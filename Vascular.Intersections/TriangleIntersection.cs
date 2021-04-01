using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Intersections
{
    /// <summary>
    /// Data about the intersection of a triangle and segment.
    /// </summary>
    public class TriangleIntersection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <param name="f"></param>
        public TriangleIntersection(Segment s, TriangleSurfaceTest t, double f)
        {
            this.Segment = s;
            this.Triangle = t;
            this.Fraction = f;
            this.Outwards = s.Direction * t.Normal > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public Segment Segment { get; }

        /// <summary>
        /// 
        /// </summary>
        public TriangleSurfaceTest Triangle { get; }

        /// <summary>
        /// The fraction along <see cref="Segment.Direction"/> at which the intersection occurs.
        /// </summary>
        public double Fraction { get; }

        /// <summary>
        /// Whether <see cref="Segment.Direction"/> and <see cref="TriangleSurfaceTest.Normal"/> are aligned.
        /// </summary>
        public bool Outwards { get; }
    }
}
