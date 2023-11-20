using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Hierarchical
{
    /// <summary>
    /// Gradients of common properties: <see cref="Branch.Flow"/>, <see cref="Branch.ReducedResistance"/>.
    /// </summary>
    public class HierarchicalGradients
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        public HierarchicalGradients(Network n)
        {
            this.Network = n;
        }

        /// <summary>
        ///
        /// </summary>
        public Dictionary<Bifurcation, BifurcationGradients> Local { get; } = new Dictionary<Bifurcation, BifurcationGradients>();

        /// <summary>
        ///
        /// </summary>
        public Dictionary<HigherSplit, SplittingGradients> LocalHigher { get; } = new();

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

        private void SetCache(Branch b, double dRe_dRp, double dRe_dQp)
        {
            if (b.End is Bifurcation bf)
            {
                var d = new BifurcationGradients(bf);
                SetCache(b.Children[0], d.dRp_dR0 * dRe_dRp, dRe_dQp + dRe_dRp * d.dRp_dQ0);
                SetCache(b.Children[1], d.dRp_dR1 * dRe_dRp, dRe_dQp + dRe_dRp * d.dRp_dQ1);
                this.Local[bf] = d;
            }
            else if (b.End is HigherSplit hs)
            {
                var d = new SplittingGradients(hs);
                this.LocalHigher[hs] = d;
                for (var i = 0; i < b.Children.Length; ++i)
                {
                    SetCache(b.Children[i], d.dRp_dRi[i] * dRe_dRp, dRe_dQp + dRe_dRp * d.dRp_dQi[i]);
                }
            }
            this.Global[b] = new BranchGradients(dRe_dRp, dRe_dQp);
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
        /// <param name="hs"></param>
        /// <returns></returns>
        public Vector3 PositionGradient(HigherSplit hs)
        {
            var p = hs.Upstream;
            var gd = this.LocalHigher[hs];
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Vector3 PositionGradient(Terminal t)
        {
            var br = t.Upstream;
            var gb = this.Global[br];
            var dp = t.Parent.Direction;
            var lp = t.Parent.Length;
            var dL_dx = dp / lp;
            return gb.dRe_dR * dL_dx;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Vector3 PositionGradient(Source s)
        {
            var br = s.Downstream[0];
            var gb = this.Global[br];
            var dc = s.Child.Direction;
            var lp = s.Child.Length;
            var dL_dx = -dc / lp;
            return gb.dRe_dR * dL_dx;
        }
    }
}
