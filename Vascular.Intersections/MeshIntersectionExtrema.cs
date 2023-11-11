using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Intersections
{
    /// <summary>
    /// When testing a segment against a mesh, record the first exit and last entry.
    /// </summary>
    public class MeshIntersectionExtrema
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="ti"></param>
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

        /// <summary>
        ///
        /// </summary>
        public Segment Segment { get; }

        /// <summary>
        ///
        /// </summary>
        public TriangleSurfaceTest? LastIn { get; private set; }

        /// <summary>
        /// The fraction along <see cref="Segment"/> at which <see cref="LastIn"/> is hit.
        /// </summary>
        public double InFraction { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public TriangleSurfaceTest? FirstOut { get; private set; }

        /// <summary>
        /// The fraction along <see cref="Segment"/> at which <see cref="FirstOut"/> is hit.
        /// </summary>
        public double OutFraction { get; private set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ti"></param>
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
