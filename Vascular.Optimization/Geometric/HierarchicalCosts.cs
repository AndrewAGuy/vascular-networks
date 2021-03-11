using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric
{
    public class HierarchicalCosts
    {
        private readonly HierarchicalGradients hierarchicalGradients;
        private readonly FluidMechanicalWork fluidMechanicalWork;
        public double WorkFactor { get; set; } = 0.0;
        private readonly List<SchreinerCost> schreinerCosts = new List<SchreinerCost>();

        public HierarchicalCosts(Network network)
        {
            hierarchicalGradients = new HierarchicalGradients()
            {
                Source = network.Source
            };
            fluidMechanicalWork = new FluidMechanicalWork(hierarchicalGradients);
        }

        public HierarchicalCosts AddSchreinerCost(double lengthExponent, double radiusExponent, double factor)
        {
            schreinerCosts.Add(new SchreinerCost(factor,
                new EffectiveLengths(radiusExponent, lengthExponent, hierarchicalGradients)));
            return this;
        }

        public Func<IDictionary<IMobileNode, Vector3>> Wrapper => () => Evaluate().gradients;

        public (double cost, IDictionary<IMobileNode, Vector3> gradients) Evaluate()
        {
            hierarchicalGradients.SetCache();
            var total = 0.0;
            if (this.WorkFactor != 0.0)
            {
                fluidMechanicalWork.SetCache();
                total = fluidMechanicalWork.Cost * this.WorkFactor;
            }

            foreach (var schreinerCost in schreinerCosts)
            {
                schreinerCost.EffectiveLengths.SetLengths();
                schreinerCost.EffectiveLengths.SetGradients();
                schreinerCost.SetCache();
                total += schreinerCost.Cost * schreinerCost.Multiplier;
            }

            var initialize = this.WorkFactor != 0.0
                ? new Func<IMobileNode, Vector3>(n => fluidMechanicalWork.PositionGradient(n) * this.WorkFactor)
                : n => Vector3.ZERO;

            var network = hierarchicalGradients.Source.Network;
            var gradients = new Dictionary<IMobileNode, Vector3>(network.Nodes.Count());
            foreach (var node in network.Nodes)
            {
                if (node is IMobileNode mobile)
                {
                    var gradient = initialize(mobile);
                    foreach (var schreinerCost in schreinerCosts)
                    {
                        gradient += schreinerCost.PositionGradient(mobile) * schreinerCost.Multiplier;
                    }
                    gradients[mobile] = gradient;
                }
            }

            return (total, gradients);
        }
    }
}
