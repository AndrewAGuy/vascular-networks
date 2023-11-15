using System;

namespace Vascular.Geometry.Generators
{
    /// <summary>
    /// Uses a Box-Muller transform to turn unit uniform numbers to Gaussian.
    /// </summary>
    public class GaussianRandom : IVector3Generator
    {
        private readonly Random random;
        private double cached = 0;
        private bool generate = true;

        /// <summary>
        ///
        /// </summary>
        /// <param name="seed"></param>
        public GaussianRandom(int seed)
        {
            random = new Random(seed);
        }

        /// <summary>
        ///
        /// </summary>
        public GaussianRandom()
        {
            random = new Random();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double NextDouble()
        {
            if (!generate)
            {
                generate = true;
                return cached;
            }

            generate = false;

            // Box-Muller transform
            var u = 1 - random.NextDouble();
            var v = 1 - random.NextDouble();
            var uu = Math.Sqrt(-2.0 * Math.Log(u));
            var vv = 2.0 * Math.PI * v;

            cached = uu * Math.Sin(vv);
            return uu * Math.Cos(vv);
        }

        /// <inheritdoc/>
        public Vector3 NextVector3()
        {
            return new Vector3(NextDouble(), NextDouble(), NextDouble());
        }
    }
}
