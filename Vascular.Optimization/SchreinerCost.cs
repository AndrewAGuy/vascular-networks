using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization
{
    public class SchreinerCost
    {
        public SchreinerCost(double a, EffectiveLengths e)
        {
            this.Multiplier = a;
            this.EffectiveLengths = e;
            this.Cache = e.Cache;
        }

        public double Multiplier { get; }
        public EffectiveLengths EffectiveLengths { get; }
        public HierarchicalGradients Cache { get; }

        public double Cost => this.EffectiveLengths.Value * dC_dLe;

        private double dC_dLe, dC_dRe, dC_dQe;

        public void SetCache()
        {
            var (dr_dR, dr_dQ) = this.EffectiveLengths.Cache.RadiusGradients;
            dC_dLe = Math.Pow(this.Cache.Source.RootRadius, this.EffectiveLengths.ExpR);
            var c = this.EffectiveLengths.ExpR * Math.Pow(this.Cache.Source.RootRadius, this.EffectiveLengths.ExpDR) * this.EffectiveLengths.Value;
            dC_dRe = c * dr_dR;
            dC_dQe = c * dr_dQ;
        }

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
