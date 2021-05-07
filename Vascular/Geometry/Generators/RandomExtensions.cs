using System;

namespace Vascular.Geometry.Generators
{
    /// <summary>
    /// 
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// Uses a random to seed another.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Random NextRandom(this Random r)
        {
            return new Random(r.Next());
        }

        /// <summary>
        /// Returns a predicate that wraps <paramref name="r"/> and returns <see langword="true"/> with probability <paramref name="p"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="r"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Func<T, bool> Bernoulli<T>(this Random r, double p)
        {
            return t => r.NextDouble() < p;
        }

        /// <summary>
        /// Returns a predicate that wraps <paramref name="r"/> and returns <see langword="true"/> with probability <paramref name="p"/>(t).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="r"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Func<T, bool> Bernoulli<T>(this Random r, Func<T, double> p)
        {
            return t => r.NextDouble() < p(t);
        }

        /// <summary>
        /// Wraps an inverse CDF function and a generator.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="invCDF"></param>
        /// <returns></returns>
        public static Func<double> InverseCDF(this Random r, Func<double, double> invCDF)
        {
            return () => invCDF(r.NextDouble());
        }
    }
}
