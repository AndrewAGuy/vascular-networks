using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Generators
{
    [Serializable]
    public class GaussianRandom : IVector3Generator
    {
        private readonly Random random;
        private double cached = 0;
        private bool generate = true;

        public GaussianRandom(int seed)
        {
            random = new Random(seed);
        }

        public GaussianRandom()
        {
            random = new Random();
        }

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

        public Vector3 NextVector3()
        {
            return new Vector3(NextDouble(), NextDouble(), NextDouble());
        }
    }
}
