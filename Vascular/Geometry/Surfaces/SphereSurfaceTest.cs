using Vascular.Geometry.Bounds;

namespace Vascular.Geometry.Surfaces
{
    /// <summary>
    /// Helper methods for sphere surfaces, since <see cref="SegmentSurfaceTest"/> will yield <see cref="double.NaN"/>
    /// for segments with start and end at the same location.
    /// </summary>
    public class SphereSurfaceTest : IAxialBoundable
    {
        private readonly Vector3 position;
        private readonly double radius;
        private readonly AxialBounds bounds;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="r"></param>
        public SphereSurfaceTest(Vector3 p, double r)
        {
            position = new(p);
            radius = r;
            bounds = new(p, r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double DistanceToSurface(Vector3 v)
        {
            return Vector3.Distance(position, v) - radius;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public (double d, Vector3 n) DistanceAndNormalToSurface(Vector3 v)
        {
            var dir = v - position;
            var dist = dir.Length;
            return dist == 0
                ? (-radius, null)
                : (dist - radius, dir / dist);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="d"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public double Overlap(Vector3 s, Vector3 d, double r)
        {
            var f = LinearAlgebra.LineFactor(s, d, position);
            var c = s + f.Clamp(0, 1) * d;
            return radius + r - Vector3.Distance(position, c);
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Position => position;

        /// <summary>
        /// 
        /// </summary>
        public double Radius => radius;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AxialBounds GetAxialBounds()
        {
            return bounds;
        }
    }
}
