using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    /// <summary>
    /// Represents a collection of costs that have hierarchical gradients and update rules.
    /// </summary>
    public class HierarchicalCosts
    {
        private readonly HierarchicalGradients hierarchicalGradients;
        private readonly FluidMechanicalWork fluidMechanicalWork;

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
        public HierarchicalCosts(Network network)
        {
            hierarchicalGradients = new HierarchicalGradients()
            {
                Source = network.Source
            };
            fluidMechanicalWork = new FluidMechanicalWork(hierarchicalGradients);
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
        public Func<IDictionary<IMobileNode, Vector3>> Wrapper => () => Evaluate().gradients;

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
        /// Estimates the change in cost for bifurcating from <paramref name="existing"/> into <paramref name="candidate"/>
        /// with the bifurcation placed at <paramref name="bifurcationPosition"/>. Works out the change in reduced resistance,
        /// flow rate and effective lengths and then computes a first order estimate of the change in cost.
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="candidate"></param>
        /// <param name="bifurcationPosition"></param>
        /// <returns></returns>
        public double EstimatedChange(Branch existing, Terminal candidate, Vector3 bifurcationPosition)
        {
            var xP = existing.Start.Position;
            var x1 = existing.End.Position;
            var x2 = candidate.Position;
            var xB = bifurcationPosition;
            var LP = (xP - xB).Length;
            var L1 = (x1 - xB).Length;
            var L2 = (x2 - xB).Length;
            var Q1 = existing.Flow;
            var Q2 = candidate.Flow;
            var RS1 = L1;
            var RS2 = L2 + existing.End.ReducedResistance;
            var (f1, f2) = candidate.Network.Splitting.Fractions(RS1, Q1, RS2, Q2);
            var RSP = LP + 1.0 / 
                (Math.Pow(f1, 4) / RS1 
                + Math.Pow(f2, 4) / RS2);
            var dR = RSP - existing.ReducedResistance;
            var dQ = Q2;

            var dC = this.WorkFactor != 0
                ? this.WorkFactor * (fluidMechanicalWork.FlowGradient(existing) * dQ
                    + fluidMechanicalWork.ReducedResistanceGradient(existing) * dR)
                : 0.0;
            foreach (var sc in schreinerCosts)
            {
                var el = sc.EffectiveLengths;
                var LS = el.Values[existing];
                var LS2 = Math.Pow(L2, el.ExpL);
                var LS1 = LS
                    - Math.Pow(existing.Length, el.ExpL)
                    + Math.Pow(L1, el.ExpL);
                var LSP = Math.Pow(LP, el.ExpL) 
                    + Math.Pow(f1, el.ExpR) * LS1 
                    + Math.Pow(f2, el.ExpR) * LS2;
                var dL = LSP - LS;
                var dS = sc.FlowGradient(existing) * dQ
                    + sc.ReducedResistanceGradient(existing) * dR
                    + sc.EffectiveLengthGradient(existing) * dL;
                dC += dS * sc.Multiplier;
            }
            return dC;
        }
    }
}
