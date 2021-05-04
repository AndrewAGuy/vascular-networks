using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric
{
    /// <summary>
    /// Wraps a collection of sources of gradients, and tries to minimize this collection.
    /// </summary>
    public class GradientDescentMinimizer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        public GradientDescentMinimizer(Network network)
        {
            this.Network = network;
        }

        /// <summary>
        /// Sets the stride so that the maximum step taken is <paramref name="s"/>.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        public static double InitialMaxStrideFactor(IEnumerable<Vector3> v, double s)
        {
            var V = Math.Sqrt(v.Max(x => x * x));
            return s / V;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool ThrowIfNotFinite { get; set; } = false;

        private void Move(IDictionary<IMobileNode, Vector3> gradient)
        {
            foreach (var kv in gradient)
            {
                if (!kv.Value.IsFinite)
                {
                    if (this.ThrowIfNotFinite)
                    {
                        throw new PhysicalValueException("Gradient is NaN or infinity");
                    }
                }
                else if (this.Predicate != null)
                {
                    var delta = -this.Stride * kv.Value;
                    if (this.Predicate(kv.Key, delta))
                    {
                        kv.Key.Position += delta;
                    }
                }
                else if (this.MovingPredicate(kv.Key))
                {
                    kv.Key.Position -= this.Stride * kv.Value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        public GradientDescentMinimizer Add(Func<Network, (double c, IDictionary<IMobileNode, Vector3> g)> cost)
        {
            costs.Add(cost);
            return this;
        }

        /// <summary>
        /// Retains the legacy interface by returning <see cref="double.NaN"/> as the cost, which is ignored.
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        public GradientDescentMinimizer Add(Func<Network, IDictionary<IMobileNode, Vector3>> cost)
        {
            costs.Add(n => (double.NaN, cost(n)));
            return this;
        }

        private readonly List<Func<Network, (double c, IDictionary<IMobileNode, Vector3> g)>> costs = new();

        /// <summary>
        /// 
        /// </summary>
        public Predicate<IMobileNode> MovingPredicate { get; set; } = n => true;

        /// <summary>
        /// If set, overrides <see cref="MovingPredicate"/> and also considers the direction of movement.
        /// </summary>
        public Func<IMobileNode, Vector3, bool> Predicate { get; set; }

        /// <summary>
        /// Sets <see cref="Predicate"/> to avoid moving nodes to within <paramref name="lMin"/> of
        /// terminals if they are connected, to prevent <see cref="double.NaN"/> from propagating up
        /// the chain. Does not prevent short vessels which may lead to nonfinite gradients.
        /// </summary>
        /// <param name="lMin"></param>
        /// <returns></returns>
        public GradientDescentMinimizer AvoidShortTerminals(double lMin)
        {
            var lMin2 = lMin * lMin;
            this.Predicate = (n, d) =>
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
            return this;
        }

        private IDictionary<IMobileNode, Vector3> CalculateGradient()
        {
            var gradients = new Dictionary<IMobileNode, Vector3>(this.Network.Segments.Count());
            this.Cost = double.NaN;
            foreach (var cost in costs)
            {
                var (c, G) = cost(this.Network);
                if (!double.IsNaN(c))
                {
                    this.Cost = double.IsNaN(this.Cost) ? c : this.Cost + c;
                }
                foreach (var g in G)
                {
                    gradients.AddOrUpdate(g.Key, g.Value, v => v, (u, v) => u + v);
                }
            }
            return gradients;
        }

        /// <summary>
        /// 
        /// </summary>
        public double Cost { get; private set; }

        /// <summary>
        /// Set to 0 to get the stride reset.
        /// </summary>
        public double Stride { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Network Network { get; set; }

        /// <summary>
        /// Tries to ensure that stability is maintained.
        /// </summary>
        public double TargetStep { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool UpdateStrideToTarget { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        public int BlockLength { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double BlockRatio { get; set; }

        /// <summary>
        /// The number of iterations at the current step.
        /// </summary>
        public int Iteration { get; private set; } = 0;

        /// <summary>
        /// Sets <see cref="Iteration"/> to 0;
        /// </summary>
        public void ResetIteration()
        {
            this.Iteration = 0;
        }

        private void EndIteration()
        {
            this.Iteration++;
            if (this.BlockLength <= 0)
            {
                return;
            }
            if (this.Iteration == this.BlockLength)
            {
                this.Stride *= this.BlockRatio;
                this.Iteration = 0;
            }
        }

        /// <summary>
        /// Return true to signal optimization is complete.
        /// </summary>
        public Predicate<double> TerminationPredicate { get; set; } = d => false;

        /// <summary>
        /// For samples &gt; 2 and order &gt; 1, fits a curve of v = v0 + a k^-<paramref name="order"/>
        /// using the previous <paramref name="samples"/>, where k is the current value of <see cref="Iteration"/>.
        /// If v is within <paramref name="fraction"/> tolerance of v0, signals to terminate.
        /// <para/>
        /// Otherwise, uses a simple test that tracks the previous value and returns true if the difference between the
        /// two is within fractional tolerance.
        /// </summary>
        /// <param name="fraction"></param>
        /// <param name="samples"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public GradientDescentMinimizer UseConvergenceTest(double fraction, int samples, int order = 1)
        {
            if (samples < 2 || order < 1)
            {
                var previous = double.PositiveInfinity;
                this.TerminationPredicate = v =>
                {
                    var v0 = previous;
                    previous = v;
                    return Math.Abs(v - v0) / Math.Abs(v0) <= fraction;
                };
            }
            else
            {
                var buffer = new CircularBuffer<double>(samples);
                this.TerminationPredicate = v =>
                {
                    buffer.Add(v);
                    if (this.Iteration < samples)
                    {
                        return false;
                    }
                    // Fit curve of V = V0 + a * n^-k
                    // If we are within fractional tolerance of V0, terminate
                    var power = Enumerable.Range(this.Iteration - samples + 1, samples)
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

            return this;
        }

        /// <summary>
        /// Calculates gradients, updates strides if needed, moves and recalculates.
        /// Returns true if termination condidtion is hit.
        /// </summary>
        public bool Iterate()
        {
            var gradient = CalculateGradient();
            if (gradient.Count == 0)
            {
                return this.ResultOnError;
            }

            if (this.Stride == 0)
            {
                this.Stride = InitialMaxStrideFactor(gradient.Values, this.TargetStep);
                this.Iteration = 0;
            }
            else if (this.UpdateStrideToTarget)
            {
                ClampStride(gradient.Values);
            }

            if (!double.IsFinite(this.Stride))
            {
                this.Stride = 0;
                return this.ResultOnError;
            }

            Move(gradient);
            this.Network.Source.CalculatePhysical();
            if (this.PropagateRadii)
            {
                this.Network.Source.PropagateRadiiDownstream();
            }
            EndIteration();
            return this.TerminationPredicate(this.Cost);
        }

        /// <summary>
        /// If the gradient returns no valid components or <see cref="Stride"/> ends up being nonfinite,
        /// the result to return. In most cases, returning true to signal termination is the best action.
        /// </summary>
        public bool ResultOnError { get; set; } = true;

        /// <summary>
        /// Whether to propagate radii downstream after making an action. Defaults to true to preserve
        /// legacy behaviour.
        /// </summary>
        public bool PropagateRadii { get; set; } = true;

        private void ClampStride(IEnumerable<Vector3> G)
        {
            var g = Math.Sqrt(G.Max(x => x * x));
            if (g * this.Stride > this.TargetStep)
            {
                this.Stride = this.TargetStep / g;
            }
        }
    }
}
