using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Vascular
{
    [DataContract]
    public class Summary
    {
        [DataMember]
        public double Mean { get; }
        [DataMember]
        public double Variance { get; }
        [DataMember]
        public double[] StandardizedMoments { get; }
        [DataMember]
        public double[] OrderStatistics { get; }

        public Summary(IEnumerable<double> values, IEnumerable<int> standardizedMoments = null, IEnumerable<double> orders = null)
        {
            this.Mean = values.Average();
            this.Variance = values.Average(x => x * x) - Math.Pow(this.Mean, 2);
            this.StandardizedMoments = standardizedMoments?.Select(
                m => values.Average(x => Math.Pow(x - this.Mean, m)) / Math.Pow(this.Variance, m * 0.5))
                .ToArray();
            if (orders != null)
            {
                var s = values.OrderBy(x => x).ToList();
                this.OrderStatistics = orders?.Select(
                    f =>
                    {
                        var fi = (s.Count - 1) * f;
                        var i = (int)Math.Floor(fi);
                        var I = (int)Math.Ceiling(fi);
                        return s[i] + (fi - i) * (s[I] - s[i]);
                    }).ToArray();
            }
        }

        public static IEnumerable<double> Quantiles(int n)
        {
            var d = 1.0 / n;
            return Enumerable.Range(0, n + 1).Select(x => x * d);
        }

        public static IEnumerable<int> MomentsFromThird(int n)
        {
            return Enumerable.Range(3, n);
        }
    }
}
