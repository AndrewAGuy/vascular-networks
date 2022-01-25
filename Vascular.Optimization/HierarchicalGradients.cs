using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization
{
    /// <summary>
    /// Gradients of common properties: <see cref="Branch.Flow"/>, <see cref="Branch.ReducedResistance"/>.
    /// </summary>
    public class HierarchicalGradients
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Bifurcation, BifurcationGradients> Local { get; } = new Dictionary<Bifurcation, BifurcationGradients>();

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Branch, BranchGradients> Global { get; } = new Dictionary<Branch, BranchGradients>();

        /// <summary>
        /// 
        /// </summary>
        public Network Network { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Source Source => this.Network.Source;

        /// <summary>
        /// 
        /// </summary>
        public Branch Root => this.Source.Child.Branch;

        private double dr_dR, dr_dQ;

        /// <summary>
        /// 
        /// </summary>
        public (double dr_dR, double dr_dQ) RadiusGradients => (dr_dR, dr_dQ);

        private void SetRadius()
        {
            if (this.Source is RadiusSource)
            {
                (dr_dR, dr_dQ) = (0, 0);
            }
            else if (this.Source is PressureSource p)
            {
                var rq_p = p.ReducedResistance * p.Flow / p.Pressure;
                var c = 0.25 * Math.Pow(rq_p, -0.75) / p.Pressure;
                (dr_dR, dr_dQ) = (c * p.Flow, c * p.ReducedResistance);
            }
            else
            {
                throw new TopologyException("Unrecognized root node");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetCache()
        {
            this.Local.Clear();
            this.Global.Clear();
            SetRadius();
            SetCache(this.Root, 1, 0);
        }

        private void SetCache(Branch b, double RR, double RQ)
        {
            if (b.End is Bifurcation bf)
            {
                var d = new BifurcationGradients(bf);
                SetCache(b.Children[0], d.dRp_dR0 * RR, d.dRp_dR0 * RQ + d.dRp_dQ0);
                SetCache(b.Children[1], d.dRp_dR1 * RR, d.dRp_dR1 * RQ + d.dRp_dQ1);
                this.Local[bf] = d;
            }
            this.Global[b] = new BranchGradients(RR, RQ);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bf"></param>
        /// <returns></returns>
        public Vector3 PositionGradient(Bifurcation bf)
        {
            var p = bf.Upstream;
            var gd = this.Local[bf];
            var gp = this.Global[p];
            return gp.dRe_dR * gd.dRp_dx;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tr"></param>
        /// <returns></returns>
        public Vector3 PositionGradient(Transient tr)
        {
            var br = tr.Parent.Branch;
            var gb = this.Global[br];
            var dp = tr.Parent.Direction;
            var lp = tr.Parent.Length;
            var dc = tr.Child.Direction;
            var lc = tr.Child.Length;
            var dL_dx = dp / lp - dc / lc;
            return gb.dRe_dR * dL_dx;
        }
    }
}
