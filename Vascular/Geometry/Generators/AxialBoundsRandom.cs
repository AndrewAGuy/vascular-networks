using System;
using Vascular.Geometry.Bounds;

namespace Vascular.Geometry.Generators
{
    /// <summary>
    /// Generates uniformly in an <see cref="AxialBounds"/>.
    /// </summary>
    public class AxialBoundsRandom : IVector3Generator
    {
        private readonly Random random;
        private readonly Vector3 lower;
        private readonly Vector3 range;

        /// <summary>
        ///
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="random"></param>
        public AxialBoundsRandom(AxialBounds bounds, Random? random = null)
        {
            this.random = random ?? new();
            lower = bounds.Lower;
            range = bounds.Range;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public Vector3 NextVector3()
        {
            var dx = range.x * random.NextDouble();
            var dy = range.y * random.NextDouble();
            var dz = range.z * random.NextDouble();
            return lower + new Vector3(dx, dy, dz);
        }
    }
}
