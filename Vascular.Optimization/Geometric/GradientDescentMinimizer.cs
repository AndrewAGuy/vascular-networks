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
                else if (this.MovingPredicate(kv.Key))
                {
                    var delta = -this.Stride * kv.Value;
                    kv.Key.Position += delta;
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

        private readonly List<Func<Network, (double c, IDictionary<IMobileNode, Vector3> g)>> costs = new();

        /// <summary>
        /// 
        /// </summary>
        public Predicate<IMobileNode> MovingPredicate { get; set; } = n => true;

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

        private int iteration = 0;

        private void EndIteration()
        {
            if (this.BlockLength <= 0)
            {
                return;
            }
            iteration++;
            if (iteration == this.BlockLength)
            {
                this.Stride *= this.BlockRatio;
                iteration = 0;
            }
        }

        /// <summary>
        /// Calculates gradients, updates strides if needed, moves and recalculates.
        /// </summary>
        public void Iterate()
        {
            var gradient = CalculateGradient();
            if (gradient.Count == 0)
            {
                return;
            }

            if (this.Stride == 0)
            {
                this.Stride = InitialMaxStrideFactor(gradient.Values, this.TargetStep);
                iteration = 0;
            }
            else if (this.UpdateStrideToTarget)
            {
                ClampStride(gradient.Values);
            }

            Move(gradient);
            this.Network.Source.CalculatePhysical();
            this.Network.Source.PropagateRadiiDownstream();
            EndIteration();
        }

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
