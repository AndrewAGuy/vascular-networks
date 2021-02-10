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
    public class FluidMechanicalWork
    {
        public FluidMechanicalWork(HierarchicalGradients c)
        {
            this.Cache = c;
        }

        public HierarchicalGradients Cache { get; }

        public double Cost { get; private set; }

        private double dW_dQ, dW_dR;

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

        public double FlowGradient(Branch br)
        {
            return dW_dQ;
        }

        public Vector3 PositionGradient(IMobileNode n)
        {
            return n switch
            {
                Bifurcation bf => this.Cache.PositionGradient(bf),
                Transient tr => this.Cache.PositionGradient(tr),
                _ => Vector3.ZERO
            } * dW_dR;
        }
    }
}
