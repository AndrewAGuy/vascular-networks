using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Generators
{
    public static class RandomExtensions
    {
        public static Random NextRandom(this Random r)
        {
            return new Random(r.Next());
        }

        public static Predicate<T> Bernoulli<T>(this Random r, double p)
        {
            return t => r.NextDouble() < p;
        }

        public static Predicate<T> Bernoulli<T>(this Random r, Func<T, double> p)
        {
            return t => r.NextDouble() < p(t);
        }

        public static void Permute<T>(this List<T> list, Random random = null)
        {
            random ??= new Random();
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                var temp = list[i];
                list[i] = list[swap];
                list[swap] = temp;
            }
        }
    }
}
