using System;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization
{
    /// <summary>
    /// A cost that is the sum over branches of length and radius powers. Allows caching of effective length terms.
    /// </summary>
    public class SchreinerCost
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="e"></param>
        public SchreinerCost(double a, EffectiveLengths e)
        {
            this.Multiplier = a;
            this.EffectiveLengths = e;
            this.Cache = e.Cache;
        }

        /// <summary>
        /// 
        /// </summary>
        public double Multiplier { get; }

        /// <summary>
        /// 
        /// </summary>
        public EffectiveLengths EffectiveLengths { get; }

        /// <summary>
        /// 
        /// </summary>
        public HierarchicalGradients Cache { get; }

        /// <summary>
        /// 
        /// </summary>
        public double Cost => this.EffectiveLengths.Value * dC_dLe;

        private double dC_dLe, dC_dRe, dC_dQe;

        /// <summary>
        /// 
        /// </summary>
        public void SetCache()
        {
            var (dr_dR, dr_dQ) = this.EffectiveLengths.Cache.RadiusGradients;
            dC_dLe = Math.Pow(this.Cache.Source.RootRadius, this.EffectiveLengths.ExpR);
            var c = this.EffectiveLengths.ExpR * Math.Pow(this.Cache.Source.RootRadius, this.EffectiveLengths.ExpDR) * this.EffectiveLengths.Value;
            dC_dRe = c * dr_dR;
            dC_dQe = c * dr_dQ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public Vector3 PositionGradient(IMobileNode n)
        {
            var (dLe_dx, dRe_dx) = n switch
            {
                Bifurcation bf => (this.EffectiveLengths.PositionGradient(bf), this.Cache.PositionGradient(bf)),
                Transient tr => (this.EffectiveLengths.PositionGradient(tr), this.Cache.PositionGradient(tr)),
                _ => (Vector3.ZERO, Vector3.ZERO)
            };
            return dLe_dx * dC_dLe 
                + dRe_dx * dC_dRe;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public double FlowGradient(Branch br)
        {
            var dLe_dQ = this.EffectiveLengths.Gradients[br].dLe_dQ;
            var dRe_dQ = this.Cache.Global[br].dRe_dQ;
            return dC_dLe * dLe_dQ 
                + dC_dRe * dRe_dQ 
                + dC_dQe;
        }
    }
}
