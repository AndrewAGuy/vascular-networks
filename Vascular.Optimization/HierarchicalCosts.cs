using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization
{
    /// <summary>
    /// Represents a collection of costs that have hierarchical gradients and update rules.
    /// </summary>
    public class HierarchicalCosts
    {
        private readonly HierarchicalGradients hierarchicalGradients;
        private readonly FluidMechanicalWork fluidMechanicalWork;

        /// <summary>
        /// 
        /// </summary>
        public HierarchicalGradients Gradients => hierarchicalGradients;

        /// <summary>
        /// 
        /// </summary>
        public FluidMechanicalWork Work => fluidMechanicalWork;

        /// <summary>
        /// Multiplier for work under HP flow assumption.
        /// </summary>
        public double WorkFactor { get; set; } = 0.0;

        private readonly List<SchreinerCost> schreinerCosts = new();

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<SchreinerCost> SchreinerCosts => schreinerCosts;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        public HierarchicalCosts()
        {
            hierarchicalGradients = new();
            fluidMechanicalWork = new FluidMechanicalWork(hierarchicalGradients);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        public HierarchicalCosts(Network network)
        {
            hierarchicalGradients = new HierarchicalGradients()
            {
                Network = network
            };
            fluidMechanicalWork = new FluidMechanicalWork(hierarchicalGradients);
        }

        /// <summary>
        /// 
        /// </summary>
        public Network Network
        {
            get => hierarchicalGradients.Network;
            set => hierarchicalGradients.Network = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lengthExponent"></param>
        /// <param name="radiusExponent"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public HierarchicalCosts AddSchreinerCost(double lengthExponent, double radiusExponent, double factor)
        {
            schreinerCosts.Add(new SchreinerCost(factor,
                new EffectiveLengths(radiusExponent, lengthExponent, hierarchicalGradients)));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double SetCache()
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
            return total;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public double FlowGradient(Branch br)
        {
            var dc_dQ = this.WorkFactor != 0.0
                ? fluidMechanicalWork.FlowGradient(br) * this.WorkFactor
                : 0.0;
            foreach (var schreinerCost in schreinerCosts)
            {
                dc_dQ += schreinerCost.FlowGradient(br) * schreinerCost.Multiplier;
            }
            return dc_dQ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public double ReducedResistanceGradient(Branch br)
        {
            var dc_dR = this.WorkFactor != 0.0
                ? fluidMechanicalWork.ReducedResistanceGradient(br) * this.WorkFactor
                : 0.0;
            foreach (var schreinerCost in schreinerCosts)
            {
                dc_dR += schreinerCost.ReducedResistanceGradient(br) * schreinerCost.Multiplier;
            }
            return dc_dR;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workFactor"></param>
        /// <param name="schreiner"></param>
        /// <returns></returns>
        public static Func<Network, Func<Network, (double, IDictionary<IMobileNode, Vector3>)>>
            Generator(double workFactor, (double l, double r, double a)[] schreiner)
        {
            return network =>
            {
                var hc = new HierarchicalCosts(network)
                {
                    WorkFactor = workFactor
                };
                foreach (var (l, r, a) in schreiner)
                {
                    hc.AddSchreinerCost(l, r, a);
                }
                return n => hc.Evaluate();
            };
        }
    }
}
