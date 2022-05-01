using System;
using Vascular.Geometry.Graphs;
using Vascular.Structure;

namespace Vascular.Functionality.Capillary
{
    /// <summary>
    /// Base type for continuous structures which aim to mimic a capillary bed.
    /// </summary>
    public abstract class CapillaryBase : Continuous<Vertex, Edge>
    {
        /// <summary>
        /// The default radius for segments. Derived classes may wish to modify radius of
        /// some segments to create hierarchy.
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<Segment, bool> PermittedIntersection { get; set; } = s => true;

        /// <summary>
        /// 
        /// </summary>
        public double MinOverlap { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override double GetRadius(Edge e)
        {
            return this.Radius;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="overlap"></param>
        /// <returns></returns>
        public override bool IsIntersectionPermitted(Segment segment, double overlap)
        {
            return this.PermittedIntersection(segment)
                && overlap >= this.MinOverlap;
        }
    }
}
