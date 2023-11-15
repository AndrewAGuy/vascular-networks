using System;
using System.Collections.Generic;
using System.Linq;

namespace Vascular
{
    /// <summary>
    /// Summary statistics, including moments and orders (quantiles).
    /// </summary>
    public class Summary
    {
        /// <summary>
        ///
        /// </summary>
        public double Mean { get; }

        /// <summary>
        ///
        /// </summary>
        public double Variance { get; }

        /// <summary>
        /// Defined as <c>Expectation[pow(X - mean, n)] / pow(std, n)</c>
        /// </summary>
        public double[]? StandardizedMoments { get; }

        /// <summary>
        ///
        /// </summary>
        public double[]? OrderStatistics { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="values"></param>
        /// <param name="standardizedMoments"></param>
        /// <param name="orders"></param>
        public Summary(IEnumerable<double> values,
            IEnumerable<int>? standardizedMoments = null, IEnumerable<double>? orders = null)
        {
            this.Mean = values.Average();
            this.Variance = values.Average(x => x * x) - Math.Pow(this.Mean, 2);
            this.StandardizedMoments = standardizedMoments?.Select(
                m => values.Average(x => Math.Pow(x - this.Mean, m)) / Math.Pow(this.Variance, m * 0.5))
                .ToArray();
            if (orders != null)
            {
                var s = values.OrderBy(x => x).ToList();
                this.OrderStatistics = orders.Select(
                    f =>
                    {
                        var fi = (s.Count - 1) * f;
                        var i = (int)Math.Floor(fi);
                        var I = (int)Math.Ceiling(fi);
                        return s[i] + (fi - i) * (s[I] - s[i]);
                    }).ToArray();
            }
        }

        /// <summary>
        /// Creates <paramref name="n"/> + 1 quantiles, equally spaced.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static IEnumerable<double> Quantiles(int n)
        {
            var d = 1.0 / n;
            return Enumerable.Range(0, n + 1).Select(x => x * d);
        }

        /// <summary>
        /// Mean and variance already computed, so create moments as { 3, ... , 3 + n - 1 }
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static IEnumerable<int> MomentsFromThird(int n)
        {
            return Enumerable.Range(3, n);
        }
    }
}
