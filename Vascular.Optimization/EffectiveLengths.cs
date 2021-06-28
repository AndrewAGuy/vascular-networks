using System;
using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization
{
    /// <summary>
    /// For <see cref="SchreinerCost"/> terms.
    /// </summary>
    public class EffectiveLengths
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="l"></param>
        /// <param name="c"></param>
        public EffectiveLengths(double r, double l, HierarchicalGradients c)
        {
            this.ExpR = r;
            this.ExpL = l;
            this.ExpDL = l - 1;
            this.ExpDR = r - 1;
            this.Cache = c;
        }

        /// <summary>
        /// 
        /// </summary>
        public double ExpR { get; }

        /// <summary>
        /// 
        /// </summary>
        public double ExpL { get; }

        /// <summary>
        /// 
        /// </summary>
        public double ExpDL { get; }

        /// <summary>
        /// 
        /// </summary>
        public double ExpDR { get; }

        /// <summary>
        /// 
        /// </summary>
        public struct RootGradient
        {
            /// <summary>
            /// 
            /// </summary>
            public double dLe_dQ;

            /// <summary>
            /// 
            /// </summary>
            public double dLe_dR;

            /// <summary>
            /// 
            /// </summary>
            public double dLe_dL;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Branch, double> Values { get; } = new();

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Branch, RootGradient> Gradients { get; } = new();

        /// <summary>
        /// 
        /// </summary>
        public HierarchicalGradients Cache { get; }

        /// <summary>
        /// 
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public void SetLengths()
        {
            this.Values.Clear();
            this.Value = Calculate(this.Cache.Root);
        }

        private double Calculate(Branch b)
        {
            var e = Math.Pow(b.Length, this.ExpL);
            if (b.End is Bifurcation bf)
            {
                var (f0, f1) = bf.Fractions;
                e += Math.Pow(f0, this.ExpR) * Calculate(bf.Downstream[0]);
                e += Math.Pow(f1, this.ExpR) * Calculate(bf.Downstream[1]);
            }
            this.Values[b] = e;
            return e;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetGradients()
        {
            this.Gradients.Clear();
            Calculate(this.Cache.Root, 0, 0, 1);
        }

        private void Calculate(Branch b, double LQ, double LR, double LL)
        {
            if (b.End is Bifurcation bf)
            {
                var dl = this.Cache.Local[bf];
                var dg = this.Cache.Global[b];
                var ls0 = this.Values[bf.Downstream[0]];
                var ls1 = this.Values[bf.Downstream[1]];
                var (f0, f1) = bf.Fractions;
                var c0 = this.ExpR * Math.Pow(f0, this.ExpDR) * ls0;
                var c1 = this.ExpR * Math.Pow(f1, this.ExpDR) * ls1;
                var dLQ = c0 * dl.df0_dQ0 + c1 * dl.df1_dQ0;
                var dLR = c0 * dl.df0_dR0 + c1 * dl.df1_dR0;
                var dLL = Math.Pow(f0, this.ExpR);
                Calculate(bf.Downstream[0],
                    dLQ + dLR * dg.dRe_dQ + dLL * LQ,
                    dLR * dg.dRe_dR + dLL * LR,
                    dLL * LL);
                dLQ = c0 * dl.df0_dQ1 + c1 * dl.df1_dQ1;
                dLR = c0 * dl.df0_dR1 + c1 * dl.df1_dR1;
                dLL = Math.Pow(f1, this.ExpR);
                Calculate(bf.Downstream[1],
                    dLQ + dLR * dg.dRe_dQ + dLL * LQ,
                    dLR * dg.dRe_dR + dLL * LR,
                    dLL * LL);
            }
            this.Gradients[b] = new RootGradient()
            {
                dLe_dL = LL,
                dLe_dQ = LQ,
                dLe_dR = LR
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bf"></param>
        /// <returns></returns>
        public Vector3 PositionGradient(Bifurcation bf)
        {
            var p = bf.Upstream;
            //var c0 = bf.Downstream[0];
            //var c1 = bf.Downstream[1];
            var gp = this.Gradients[p];
            //var g0 = this.Gradients[c0];
            //var g1 = this.Gradients[c1];
            var gd = this.Cache.Local[bf];
            var (f0, f1) = bf.Fractions;
            var ls0 = this.Values[bf.Downstream[0]];
            var ls1 = this.Values[bf.Downstream[1]];

            var dLp_dx = this.ExpL * (
                Math.Pow(gd.Lp, this.ExpDL) * gd.dLp_dx +
                Math.Pow(gd.L0, this.ExpDL) * Math.Pow(f0, this.ExpR) * gd.dL0_dx +
                Math.Pow(gd.L1, this.ExpDL) * Math.Pow(f1, this.ExpR) * gd.dL1_dx)
                + ls0 * this.ExpR * Math.Pow(f0, this.ExpDR) * gd.df0_dx
                + ls1 * this.ExpR * Math.Pow(f1, this.ExpDR) * gd.df1_dx;

            return gp.dLe_dR * gd.dRp_dx
                + gp.dLe_dL * dLp_dx;
            //+ g0.dLe_dR * gd.dL0_dx
            //+ g1.dLe_dR * gd.dL1_dx
            //+ gp.dLe_dL * this.ExpL * Math.Pow(gd.Lp, this.ExpDL) * gd.dLp_dx
            //+ g0.dLe_dL * this.ExpL * Math.Pow(gd.L0, this.ExpDL) * gd.dL0_dx
            //+ g1.dLe_dL * this.ExpL * Math.Pow(gd.L1, this.ExpDL) * gd.dL1_dx;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tr"></param>
        /// <returns></returns>
        public Vector3 PositionGradient(Transient tr)
        {
            var br = tr.Parent.Branch;
            var gb = this.Gradients[br];
            var bl = br.Length;

            var dp = tr.Parent.Direction;
            var lp = tr.Parent.Length;
            var dc = tr.Child.Direction;
            var lc = tr.Child.Length;
            var dL_dx = dp / lp - dc / lc;
            return gb.dLe_dR * dL_dx
                + gb.dLe_dL * this.ExpL * Math.Pow(bl, this.ExpDL) * dL_dx;
        }

        public void Propagate(IMobileNode n)
        {
            switch (n)
            {
                case Transient tr:
                    Propagate(tr.Parent.Branch);
                    break;
                case Bifurcation bf:
                    Update(bf.Downstream[0]);
                    Update(bf.Downstream[1]);
                    Propagate(bf.Upstream);
                    break;
            }
        }

        public double Update(Branch b)
        {
            var e = Math.Pow(b.Length, this.ExpL);
            if (b.End is Bifurcation bf)
            {
                var (f0, f1) = bf.Fractions;
                e += Math.Pow(f0, this.ExpR) * this.Values[bf.Downstream[0]];
                e += Math.Pow(f1, this.ExpR) * this.Values[bf.Downstream[1]];
            }
            this.Values[b] = e;
            return e;
        }

        public void Propagate(Branch b)
        {
            if (b.Parent is Branch p)
            {
                Update(b);
                Propagate(p);
            }
            else
            {
                this.Value = Update(b);
            }
        }
    }
}
