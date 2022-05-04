using System;
using System.Linq;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    /// <summary>
    /// Methods for setting branch profiles and ensuring that they do not distort the design flow rates.
    /// Note that profiles will reduce the optimality of <see cref="SchreinerCost"/> with radius exponent &gt;= 1.
    /// Apply these at the end of the design procedure, during the final round of collision resolution to ensure
    /// that expanded segments do not create new intersections.
    /// </summary>
    public static class Profile
    {
        /// <summary>
        /// Ensures that the resistance of the branch as defined by <see cref="Branch.Length"/> and
        /// <see cref="Branch.Radius"/> is the same as the sum over the segment components.
        /// </summary>
        /// <param name="b"></param>
        public static void Normalize(Branch b)
        {
            var T = b.Segments.Sum(s => s.Length / Math.Pow(s.Radius, 4));
            var R = b.Length / Math.Pow(b.Radius, 4);
            var F = Math.Pow(T / R, 0.25);
            foreach (var s in b.Segments)
            {
                s.Radius *= F;
            }
        }

        private static double NeighbouringAverage(Segment s, Func<Segment, double> w, bool S)
        {
            var F = 0.0;
            var W = 0.0;

            if (s.Start.Parent is Segment p)
            {
                var ww = w(p);
                F += p.Radius * ww;
                W += ww;
            }

            if (S)
            {
                foreach (var c in s.Start.Children.Where(C => C != s))
                {
                    var ww = w(c);
                    F += c.Radius * ww;
                    W += ww;
                }
            }

            if (s.End is Terminal t && t.Partners != null)
            {
                foreach (var tm in t.Partners.Where(tm => tm != t))
                {
                    var ww = w(tm.Parent);
                    F += tm.Parent.Radius * ww;
                    W += ww;
                }
            }
            else
            {
                foreach (var c in s.End.Children)
                {
                    var ww = w(c);
                    F += c.Radius * ww;
                    W += ww;
                }
            }

            return F / W;
        }

        /// <summary>
        /// Applies average radius smoothing for specified number of iterations, stepping specified 
        /// <paramref name="fraction"/> towards the average neighbouring radius each time. 
        /// Defaults to flow weighting, preventing highly asymmetric bifurcations
        /// from contracting their parent too much.
        /// Can choose to include or ignore segment siblings, as profiles are mostly intended to ensure
        /// smooth parent-child transitions.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="iterations"></param>
        /// <param name="weighting"></param>
        /// <param name="fraction"></param>
        /// <param name="siblings"></param>
        public static void Average(Network network, int iterations,
            Func<Segment, double> weighting = null, double fraction = 1, bool siblings = false)
        {
            var fractionOld = 1.0 - fraction;
            weighting ??= s => s.Flow;

            for (var i = 0; i < iterations; ++i)
            {
                var average = network.Segments
                    .Select(s => (s, NeighbouringAverage(s, weighting, siblings)))
                    .ToList();
                foreach (var (s, a) in average)
                {
                    s.Radius = s.Radius * fractionOld + a * fraction;
                }

                foreach (var b in network.Branches)
                {
                    Normalize(b);
                }
            }
        }
    }
}
