using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Hierarchical
{
    /// <summary>
    /// Represents the work done to move fluid through the tubes.
    /// </summary>
    public class PumpingWorkCost : HierarchicalCost
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="c"></param>
        /// <param name="multiplier">Captures effect of viscosity and channel shape, must account for default
        /// computation of reduced resistances using a scaled viscosity of 1.</param>
        /// <param name="flow">If non-zero, fixes the flow rate at the root, allowing intermediate growth stages
        /// to optimize for the correct final cost with respect to work.</param>
        public PumpingWorkCost(HierarchicalGradients c, double multiplier, double flow = 0.0)
        {
            this.Cache = c;
            this.flow = flow;
            this.multiplier = multiplier;
        }

        /// <summary>
        ///
        /// </summary>
        public HierarchicalGradients Cache { get; }

        /// <summary>
        ///
        /// </summary>
        public override double Cost => cost;

        private double cost;
        private double dW_dQ, dW_dR;

        private readonly double flow;
        private readonly double multiplier;

        /// <summary>
        ///
        /// </summary>
        public override void SetCache(Network n)
        {
            if (n is not null)
            {
                this.Cache.Network = n;
                this.Cache.SetCache();
            }

            var (dr_dR, dr_dQ) = this.Cache.RadiusGradients;
            var r = this.Cache.Source.RootRadius;
            var r2 = r * r;
            var r4 = r2 * r2;
            var r4i = 1.0 / r4;
            var Q = flow != 0.0 ? flow : this.Cache.Source.Flow;
            var Q2 = Q * Q;
            var R = this.Cache.Source.ReducedResistance;
            cost = Q2 * R * r4i * multiplier;
            dW_dQ = flow != 0.0 ? 0 : (2 * R * r4i * Q - 4 * cost / r * dr_dQ) * multiplier;
            dW_dR = (Q2 * r4i - 4 * cost / r * dr_dR) * multiplier;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public override double FlowGradient(Branch br)
        {
            var dRe_dQ = this.Cache.Global[br].dRe_dQ;
            return dW_dQ
                + dW_dR * dRe_dQ;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public override Vector3 PositionGradient(IMobileNode n)
        {
            return n switch
            {
                Bifurcation bf => this.Cache.PositionGradient(bf),
                Transient tr => this.Cache.PositionGradient(tr),
                Terminal t => this.Cache.PositionGradient(t),
                Source s => this.Cache.PositionGradient(s),
                HigherSplit hs => this.Cache.PositionGradient(hs),
                _ => Vector3.ZERO
            } * dW_dR;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public override double ReducedResistanceGradient(Branch br)
        {
            return dW_dR * this.Cache.Global[br].dRe_dR;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public (double dC_dQ, double dC_dR) Gradients(Branch br)
        {
            var cg = this.Cache.Global[br];
            var dC_dQ = dW_dQ
                + dW_dR * cg.dRe_dQ;
            var dC_dR = dW_dR * cg.dRe_dR;
            return (dC_dQ, dC_dR);
        }
    }
}
