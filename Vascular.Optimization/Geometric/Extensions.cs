using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Generators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Sets <see cref="GradientDescentMinimizer.Predicate"/> to avoid moving nodes to within 
        /// <paramref name="lMin"/> of terminals if they are connected, to prevent <see cref="double.NaN"/> 
        /// from propagating up the chain. Does not prevent short vessels which may lead to nonfinite gradients.
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="lMin"></param>
        /// <returns></returns>
        public static GradientDescentMinimizer AvoidShortTerminals(this GradientDescentMinimizer gd, double lMin)
        {
            var lMin2 = lMin * lMin;
            gd.Predicate = (n, d) =>
            {
                var p = n.Position + d;
                foreach (var c in n.Children)
                {
                    var b = c.Branch;
                    if (b.IsTerminal &&
                        Vector3.DistanceSquared(b.End.Position, p) <= lMin2)
                    {
                        return false;
                    }
                }
                return true;
            };
            return gd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static GradientDescentMinimizer NormalizeGradients(this GradientDescentMinimizer gd,
            Func<IMobileNode, double> scale = null)
        {
            gd.OnGradientComputed += scale == null
                ? G =>
                {
                    foreach (var (n, g) in G)
                    {
                        g.Copy(g.NormalizeSafe(0) ?? Vector3.ZERO);
                    }
                }
            : G =>
            {
                foreach (var (n, g) in G)
                {
                    g.Copy((g.NormalizeSafe(0) ?? Vector3.ZERO) * scale(n));
                }
            };
            return gd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="value"></param>
        /// <param name="isFraction"></param>
        /// <returns></returns>
        public static GradientDescentMinimizer AvoidShortVessels(this GradientDescentMinimizer gd,
            double value, bool isFraction = true)
        {
            gd.MovingPredicate = isFraction
                ? n => n switch
                {
                    Bifurcation bf => bf.MinOuterLength() * value < bf.MinInnerLength(),
                    Transient tr => tr.OuterLength() * value < tr.MinInnerLength(),
                    _ => true,
                }
                : n => n switch
                {
                    Bifurcation bf => value < bf.MinInnerLength(),
                    Transient tr => value < tr.MinInnerLength(),
                    _ => true,
                };
            return gd;
        }

        /// <summary>
        /// For samples &gt; 2 and order &gt; 1, fits a curve of v = v0 + a k^-<paramref name="order"/>
        /// using the previous <paramref name="samples"/>, where k is the current value of <see cref="GradientDescentMinimizer.Iteration"/>.
        /// If v is within <paramref name="fraction"/> tolerance of v0, signals to terminate.
        /// <para/>
        /// Otherwise, uses a simple test that tracks the previous value and returns true if the difference between the
        /// two is within fractional tolerance.
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="fraction"></param>
        /// <param name="samples"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static GradientDescentMinimizer UseConvergenceTest(this GradientDescentMinimizer gd,
            double fraction, int samples, int order = 1)
        {
            if (samples < 2 || order < 1)
            {
                var previous = double.PositiveInfinity;
                gd.TerminationPredicate = v =>
                {
                    var v0 = previous;
                    previous = v;
                    return Math.Abs(v - v0) / Math.Abs(v0) <= fraction;
                };
            }
            else
            {
                var buffer = new CircularBuffer<double>(samples);
                gd.TerminationPredicate = v =>
                {
                    buffer.Add(v);
                    if (gd.Iteration < samples)
                    {
                        return false;
                    }
                    // Fit curve of V = V0 + a * n^-k
                    // If we are within fractional tolerance of V0, terminate
                    var power = Enumerable.Range(gd.Iteration - samples + 1, samples)
                        .Select(i => Math.Pow(i, -order))
                        .ToArray();
                    // Least squares, A^t A will be 2x2
                    var a00 = (double)samples;      // unit * unit
                    var a01 = power.Sum();          // unit * power
                    var a11 = power.Dot(power);
                    var a0y = buffer.Sum();         // unit * values
                    var a1y = power.Dot(buffer);
                    // Solve (A^t A)^-1 A^t y for what we are interested in
                    var det = a00 * a11 - a01 * a01;
                    if (det == 0)
                    {
                        return false;
                    }
                    var v0 = (a11 * a0y - a01 * a1y) / det;
                    // Could solve for decay term as well, but we only care whether we're within fractional
                    // tolerance of the end result. (Hence not solving for A11)
                    return Math.Abs(v - v0) / Math.Abs(v0) <= fraction;
                };
            }

            return gd;
        }

        /// <summary>
        /// Recreates the legacy behaviour from the original ACCO implementation.
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="fStep"></param>
        /// <param name="fTerm"></param>
        /// <param name="stride"></param>
        /// <param name="fBlock"></param>
        /// <param name="nBlock"></param>
        /// <returns></returns>
        public static GradientDescentMinimizer UseLegacyBehaviourACCO(this GradientDescentMinimizer gd,
            double fStep, double fTerm, double stride, double fBlock, int nBlock)
        {
            gd.AvoidShortVessels(fTerm);
            gd.NormalizeGradients(GradientDescentMinimizer.ScaleByInnerLength(fStep));
            gd.UpdateStrideToTarget = false;
            gd.Stride = stride;
            gd.BlockLength = nBlock;
            gd.BlockRatio = fBlock;
            return gd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="generator"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static GradientDescentMinimizer RandomIfNonFinite(this GradientDescentMinimizer gd,
            IVector3Generator generator, Func<IMobileNode, double> scale = null)
        {
            scale ??= n => 1;
            void filter(IDictionary<IMobileNode, Vector3> G)
            {
                foreach (var kv in G)
                {
                    var v = kv.Value;
                    if (!v.IsFinite)
                    {
                        v.Copy(scale(kv.Key) * generator.NextVector3());
                    }
                }
            }
            gd.OnGradientComputed += filter;
            return gd;
        }
    }
}
