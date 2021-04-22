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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gradient"></param>
        /// <param name="stride"></param>
        /// <param name="predicate"></param>
        /// <param name="throwIfNotFinite"></param>
        public static void Move(IDictionary<IMobileNode, Vector3> gradient, double stride, Predicate<IMobileNode> predicate, bool throwIfNotFinite)
        {
            foreach (var kv in gradient)
            {
                if (!kv.Value.IsFinite)
                {
                    if (throwIfNotFinite)
                    {
                        throw new PhysicalValueException("Gradient is NaN or infinity");
                    }
                }
                else if (predicate(kv.Key))
                {
                    kv.Key.Position -= stride * kv.Value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cost"></param>
        /// <returns></returns>
        public GradientDescentMinimizer Add(Func<Network, IDictionary<IMobileNode, Vector3>> cost)
        {
            costs.Add(cost);
            return this;
        }

        private readonly List<Func<Network, IDictionary<IMobileNode, Vector3>>> costs = new();

        /// <summary>
        /// 
        /// </summary>
        public Predicate<IMobileNode> MovingPredicate { get; set; } = n => true;

        private IDictionary<IMobileNode, Vector3> CalculateGradient()
        {
            var gradients = new Dictionary<IMobileNode, Vector3>(this.Network.Segments.Count());
            foreach (var cost in costs)
            {
                var gradient = cost(this.Network);
                foreach (var g in gradient)
                {
                    gradients.AddOrUpdate(g.Key, g.Value, v => v, (u, v) => u + v);
                }
            }
            return gradients;
        }

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
        /// Calculates gradients, updates strides if needed, moves and recalculates.
        /// </summary>
        public void Iterate()
        {
            var gradient = CalculateGradient();
            if (this.Stride == 0)
            {
                this.Stride = InitialMaxStrideFactor(gradient.Values, this.TargetStep);
            }
            else if (this.UpdateStrideToTarget)
            {
                ClampStride(gradient.Values);
            }
            Move(gradient, this.Stride, this.MovingPredicate, this.ThrowIfNotFinite);
            this.Network.Source.CalculatePhysical();
            this.Network.Source.PropagateRadiiDownstream();
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
