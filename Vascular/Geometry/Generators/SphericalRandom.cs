using System;

namespace Vascular.Geometry.Generators
{
    /// <summary>
    /// Generates points uniformly on the unit sphere, optionally generating a radius as well.
    /// </summary>
    public class SphericalRandom : IVector3Generator
    {
        private readonly Random random;
        private readonly Func<double, double> radius;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        /// <param name="radius"></param>
        public SphericalRandom(Random random, Func<double, double> radius = null)
        {
            this.random = random;
            this.radius = radius;
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
            var s = new Vector3(x, y, z);
            if (radius == null)
            {
                return s;
            }
            r = radius(random.NextDouble());
            return s * r;
        }
    }
}
