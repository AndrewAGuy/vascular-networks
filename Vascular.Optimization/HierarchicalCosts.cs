using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

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
        public Network Network => hierarchicalGradients.Source.Network;

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
            var RS1 = L1 + existing.End.ReducedResistance;
            var RS2 = L2;
            var (f1, f2) = candidate.Network.Splitting.Fractions(RS1, Q1, RS2, Q2);
            var RSP = LP + 1.0 /
                (Math.Pow(f1, 4) / RS1
                + Math.Pow(f2, 4) / RS2);
            var dR = RSP - existing.ReducedResistance;
            var dQ = Q2;

            var dC = 0.0;
            if (this.WorkFactor != 0)
            {
                var (dW_dQ, dW_dR) = fluidMechanicalWork.Gradients(existing);
                dC = this.WorkFactor * (dW_dQ * dQ + dW_dR * dR);
            }
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
                var (dC_dQ, dC_dR, dC_dL) = sc.Gradients(existing);
                dC += sc.Multiplier * (dC_dQ * dQ + dC_dR * dR + dC_dL * dL);
            }
            return dC;
        }

        /// <summary>
        /// Estimates the change in cost using a first order approximation associated with bifurcating from
        /// <paramref name="existing"/> into <paramref name="target"/> with the new node placed at <paramref name="newPosition"/>.
        /// Similar to <see cref="EstimatedChange(Branch, Terminal, Vector3)"/>, but slightly more expensive as it retrieves downstream
        /// data for the children of <paramref name="target"/>.
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="target"></param>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        public double EstimatedChange(Branch existing, Bifurcation target, Vector3 newPosition)
        {
            var xP = existing.Start.Position;
            var x1 = existing.End.Position;
            var x2 = target.Position;
            var xB = newPosition;
            var LP = (xP - xB).Length;
            var L1 = (x1 - xB).Length;
            var L2 = (x2 - xB).Length;
            var Q1 = existing.Flow;
            var Q2 = target.Flow;
            var RS1 = L1 + existing.End.ReducedResistance;
            var RS2 = L2 + target.ReducedResistance;
            var (f1, f2) = target.Network.Splitting.Fractions(RS1, Q1, RS2, Q2);
            var RSP = LP + 1.0 /
                (Math.Pow(f1, 4) / RS1
                + Math.Pow(f2, 4) / RS2);
            var dR = RSP - existing.ReducedResistance;
            var dQ = Q2;

            var dC = 0.0;
            if (this.WorkFactor != 0)
            {
                var (dW_dQ, dW_dR) = fluidMechanicalWork.Gradients(existing);
                dC = this.WorkFactor * (dW_dQ * dQ + dW_dR * dR);
            }
            var (F0, F1) = target.Fractions;
            var D0 = target.Downstream[0];
            var D1 = target.Downstream[1];
            foreach (var sc in schreinerCosts)
            {
                var el = sc.EffectiveLengths;
                var LS = el.Values[existing];
                var LS2 = Math.Pow(L2, el.ExpL)
                    + el.Values[D0] * Math.Pow(F0, el.ExpR)
                    + el.Values[D1] * Math.Pow(F1, el.ExpR);
                var LS1 = LS
                    - Math.Pow(existing.Length, el.ExpL)
                    + Math.Pow(L1, el.ExpL);
                var LSP = Math.Pow(LP, el.ExpL)
                    + Math.Pow(f1, el.ExpR) * LS1
                    + Math.Pow(f2, el.ExpR) * LS2;
                var dL = LSP - LS;
                var (dC_dQ, dC_dR, dC_dL) = sc.Gradients(existing);
                dC += sc.Multiplier * (dC_dQ * dQ + dC_dR * dR + dC_dL * dL);
            }
            return dC;
        }

        /// <summary>
        /// Estimates the cost change associated with removing this branch, and optionally straightening the surviving branch.
        /// Uses a first order approximation.
        /// </summary>
        /// <param name="losing"></param>
        /// <param name="straighten"></param>
        /// <returns></returns>
        public double EstimatedChange(Branch losing, bool straighten = true)
        {
            if (losing.Start is not Bifurcation bf)
            {
                throw new TopologyException("Invalid start node type");
            }
            var sibling = bf.Downstream[1 - bf.IndexOf(losing)];
            var parent = bf.Upstream;

            // Calculate new L, R*; dQ and dR follow and then we can get work estimate
            var L = straighten
                ? Vector3.Distance(sibling.End.Position, parent.Start.Position)
                : sibling.Length + parent.Length;
            var RS = L + sibling.End.ReducedResistance;
            var dR = RS - parent.ReducedResistance;
            var dQ = -losing.Flow;

            var dC = 0.0;
            if (this.WorkFactor != 0)
            {
                var (dW_dQ, dW_dR) = fluidMechanicalWork.Gradients(parent);
                dC = this.WorkFactor * (dW_dQ * dQ + dW_dR * dR);
            }

            // For each Schreiner cost, get the new length and the downstream value
            foreach (var sc in schreinerCosts)
            {
                var el = sc.EffectiveLengths;
                var downLS = el.Values[sibling] - Math.Pow(sibling.Length, el.ExpL);
                var newLS = Math.Pow(L, el.ExpL) + downLS;
                var dL = newLS - el.Values[parent];

                var (dC_dQ, dC_dR, dC_dL) = sc.Gradients(parent);
                dC += sc.Multiplier * (dC_dQ * dQ + dC_dR * dR + dC_dL * dL);
            }

            return dC;
        }

        /// <summary>
        /// Estimates the cost change associated with the bifurcation moving action.
        /// </summary>
        /// <param name="moving"></param>
        /// <param name="target"></param>
        /// <param name="bifurcationPosition"></param>
        /// <returns></returns>
        public double EstimatedChange(BranchNode moving, Branch target, Vector3 bifurcationPosition)
        {
            var bfGain = moving switch
            {
                Bifurcation bf => EstimatedChange(target, bf, bifurcationPosition),
                Terminal t => EstimatedChange(target, t, bifurcationPosition),
                _ => throw new TopologyException("Invalid target node type")
            };
            var bfLost = EstimatedChange(moving.Upstream);
            return bfGain + bfLost;
        }

        /// <summary>
        /// Estimates the change in cost associated with changing the branch <paramref name="rewiring"/>
        /// to end with node <paramref name="target"/>, using a first order approximation.
        /// </summary>
        /// <param name="rewiring"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public double EstimatedChange(Branch rewiring, BranchNode target)
        {
            var Qold = rewiring.Flow;
            var Qnew = target.Flow;
            var dQ = Qnew - Qold;

            var Lnew = Vector3.Distance(rewiring.Start.Position, target.Position);
            var RSold = rewiring.ReducedResistance;
            var RSNew = Lnew + target.ReducedResistance;
            var dR = RSNew - RSold;

            var dC = 0.0;
            if (this.WorkFactor != 0)
            {
                var (dW_dQ, dW_dR) = fluidMechanicalWork.Gradients(rewiring);
                dC = this.WorkFactor * (dW_dQ * dQ + dW_dR * dR);
            }

            foreach (var sc in schreinerCosts)
            {
                var el = sc.EffectiveLengths;
                var LSold = el.Values[rewiring];
                var LSnew = Math.Pow(Lnew, el.ExpL);
                if (target is Bifurcation bf)
                {
                    var (f0, f1) = bf.Fractions;
                    var d0 = bf.Downstream[0];
                    var d1 = bf.Downstream[1];
                    LSnew += Math.Pow(f0, el.ExpR) * el.Values[d0]
                        + Math.Pow(f1, el.ExpR) * el.Values[d1];
                }
                var dL = LSnew - LSold;

                var (dC_dQ, dC_dR, dC_dL) = sc.Gradients(rewiring);
                dC += sc.Multiplier * (dC_dQ * dQ + dC_dR * dR + dC_dL * dL);
            }

            return dC;
        }

        /// <summary>
        /// Estimates the change in cost associated with the swap ends action, using 
        /// <see cref="EstimatedChange(Branch, BranchNode)"/> on both branches.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public double EstimatedChange(Branch a, Branch b)
        {
            return EstimatedChange(a, b.End) 
                + EstimatedChange(b, a.End);
        }
    }
}
