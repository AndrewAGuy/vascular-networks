using System;

namespace Vascular.Geometry.Generators
{
    /// <summary>
    /// Generates points uniformly on the unit sphere.
    /// </summary>
    public class SphericalRandom : IVector3Generator
    {
        private readonly Random random;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        public SphericalRandom(Random random)
        {
            this.random = random;
        }

        /// <inheritdoc/>
        public Vector3 NextVector3()
        {
            // Archimedes' Theorem (cylinder and sphere)
            // Pick z = cos b ~ U(-1, 1), a ~ U(0, 2pi)
            var z = random.NextDouble() * 2.0 - 1.0;
            var a = random.NextDouble() * Math.PI * 2.0;
            var r = Math.Sqrt(1.0 - z * z);
            var x = r * Math.Cos(a);
            var y = r * Math.Sin(a);
            return new Vector3(x, y, z);
        }
    }
}
