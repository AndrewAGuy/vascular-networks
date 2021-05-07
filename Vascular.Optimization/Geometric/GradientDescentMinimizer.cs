using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

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

        /// <summary>
        /// Removes all costs and allows new ones to be attached.
        /// </summary>
        /// <returns></returns>
        public GradientDescentMinimizer Clear()
        {
            costs.Clear();
            return this;
        }

        private readonly List<Func<Network, (double c, IDictionary<IMobileNode, Vector3> g)>> costs = new();

        /// <summary>
        /// Allows modifying or recording gradient entries.
        /// </summary>
        public Action<IDictionary<IMobileNode, Vector3>> OnGradientComputed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<IMobileNode, bool> MovingPredicate { get; set; } = n => true;

        /// <summary>
        /// If set, overrides <see cref="MovingPredicate"/> and also considers the direction of movement.
        /// </summary>
        public Func<IMobileNode, Vector3, bool> Predicate { get; set; }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Func<IMobileNode, double> ScaleByInnerLength(double scale)
        {
            return n => n switch
                {
                    Bifurcation bf => bf.MinInnerLength() * scale,
                    Transient tr => tr.MinInnerLength() * scale,
                    _ => 1,
                };
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
        public Func<double, bool> TerminationPredicate { get; set; } = d => false;      

        /// <summary>
        /// Calculates gradients, updates strides if needed, moves and recalculates.
        /// Returns true if termination condidtion is hit.
        /// </summary>
        public bool Iterate()
        {
            var gradient = CalculateGradient();
            this.OnGradientComputed?.Invoke(gradient);
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
