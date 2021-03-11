using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    public class GradientDescentMinimizer
    {
        public GradientDescentMinimizer(Network network)
        {
            this.Network = network;
        }

        public static double InitialMaxStrideFactor(IEnumerable<Vector3> v, double s)
        {
            var V = Math.Sqrt(v.Max(x => x * x));
            return s / V;
        }

        public static void Move(IDictionary<IMobileNode, Vector3> gradient, double stride, Predicate<IMobileNode> predicate)
        {
            foreach (var kv in gradient)
            {
                if (predicate(kv.Key))
                {
                    kv.Key.Position -= stride * kv.Value;
                }
            }
        }

        public GradientDescentMinimizer Add(Func<Network, IDictionary<IMobileNode, Vector3>> cost)
        {
            costs.Add(cost);
            return this;
        }

        private readonly List<Func<Network, IDictionary<IMobileNode, Vector3>>> costs
            = new List<Func<Network, IDictionary<IMobileNode, Vector3>>>();

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

        public double Stride { get; set; }
        public Network Network { get; set; }

        public double TargetStep { get; set; }

        public void Iterate()
        {
            var gradient = CalculateGradient();
            if (this.Stride == 0)
            {
                this.Stride = InitialMaxStrideFactor(gradient.Values, this.TargetStep);
            }
            Move(gradient, this.Stride, this.MovingPredicate);
            this.Network.Source.CalculatePhysical();
            this.Network.Source.PropagateRadiiDownstream();
        }

        //public GradientDescentMinimizer Add(Func<Network,IDictionary<IMobileNode,Vector3>> g, )

        //public GradientDescentMinimizer Add(Func<Network, IDictionary<IMobileNode, Vector3>> g, double s)
        //{

        //}
    }
}
