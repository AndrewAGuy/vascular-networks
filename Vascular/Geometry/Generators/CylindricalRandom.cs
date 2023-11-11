using System;

namespace Vascular.Geometry.Generators
{
    /// <summary>
    /// Generates points uniformly on the unit disc, optionally generating a radius and height as well.
    /// </summary>
    public class CylindricalRandom : IVector3Generator
    {
        private readonly Random random;
        private readonly Func<double, double>? radius;
        private readonly Func<double, double>? height;

        /// <summary>
        ///
        /// </summary>
        /// <param name="random"></param>
        /// <param name="radius"></param>
        /// <param name="height"></param>
        public CylindricalRandom(Random random, Func<double, double>? radius = null,
            Func<double, double>? height = null)
        {
            this.random = random;
            this.radius = radius;
            this.height = height;
        }

        /// <summary>
        /// Sets the radius sampling to generate uniform sampling by area.
        /// Uses inverse CDF, with F(r) = (r/R)^2.
        /// Sets height samping to uniform in range [-h,h].
        /// </summary>
        /// <param name="random"></param>
        /// <param name="radius"></param>
        /// <param name="height"></param>
        public CylindricalRandom(Random random, double radius, double height)
        {
            this.random = random;
            this.radius = F => radius * Math.Pow(F, 1.0 / 2.0);
            this.height = F => height * 2.0 * (F - 0.5);
        }

        /// <inheritdoc/>
        public Vector3 NextVector3()
        {
            var a = random.NextDouble() * Math.PI * 2.0;
            var x = new Vector3(Math.Cos(a), Math.Sin(a), 0);
            if (radius != null)
            {
                var r= radius(random.NextDouble());
                x.x *= r;
                x.y *= r;
            }
            if (height != null)
            {
                x.z = height(random.NextDouble());
            }
            return x;
        }
    }
}
