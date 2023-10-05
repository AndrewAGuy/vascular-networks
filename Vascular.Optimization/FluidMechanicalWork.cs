using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;
using Vascular.Optimization.Hierarchical;
using System;

namespace Vascular.Optimization
{
    /// <summary>
    /// Represents the work done to move fluid through the tubes.
    /// </summary>
    [Obsolete]
    public class FluidMechanicalWork
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="c"></param>
        public FluidMechanicalWork(HierarchicalGradients c)
        {
            this.Cache = c;
        }

        /// <summary>
        ///
        /// </summary>
        public HierarchicalGradients Cache { get; }

        /// <summary>
        ///
        /// </summary>
        public double Cost { get; private set; }

        private double dW_dQ, dW_dR;

        /// <summary>
        ///
        /// </summary>
        public void SetCache()
        {
            var (dr_dR, dr_dQ) = this.Cache.RadiusGradients;
            var r = this.Cache.Source.RootRadius;
            var r2 = r * r;
            var r4 = r2 * r2;
            var r4i = 1.0 / r4;
            var Q = this.Cache.Source.Flow;
            var Q2 = Q * Q;
            var R = this.Cache.Source.ReducedResistance;
            this.Cost = Q2 * R * r4i;
            dW_dQ = 2 * R * r4i * Q - 4 * this.Cost / r * dr_dQ;
            dW_dR = Q2 * r4i - 4 * this.Cost / r * dr_dR;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public double FlowGradient(Branch br)
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
        public Vector3 PositionGradient(IMobileNode n)
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
        public double ReducedResistanceGradient(Branch br)
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
