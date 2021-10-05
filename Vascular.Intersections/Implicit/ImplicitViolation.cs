using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Intersections.Implicit
{
    /// <summary>
    /// 
    /// </summary>
    public class ImplicitViolation
    {
        /// <summary>
        /// 
        /// </summary>
        public INode Node { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Gradient { get; set; }
    }
}
