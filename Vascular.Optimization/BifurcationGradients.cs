using System;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization
{
    public class BifurcationGradients
    {
        public BifurcationGradients(Bifurcation bf)
        {
            var sr = bf.Network.Splitting;
            var bx = bf.Position;
            var px = bf.Parent.Start.Position;
            var c0x = bf.Children[0].End.Position;
            var c1x = bf.Children[1].End.Position;
            var Dp = bx - px;
            var D0 = bx - c0x;
            var D1 = bx - c1x;
            this.Lp = bf.Upstream.Length;
            this.L0 = bf.Downstream[0].Length;
            this.L1 = bf.Downstream[1].Length;
            this.dLp_dx = Dp / Dp.Length;
            this.dL0_dx = D0 / D0.Length;
            this.dL1_dx = D1 / D1.Length;

            var rs0 = bf.Downstream[0].ReducedResistance;
            var rs1 = bf.Downstream[1].ReducedResistance;
            var Q0 = bf.Downstream[0].Flow;
            var Q1 = bf.Downstream[1].Flow;

            (this.df0_dR0, this.df1_dR0) = sr.ReducedResistanceGradient(rs0, Q0, rs1, Q1);
            (this.df1_dR1, this.df0_dR1) = sr.ReducedResistanceGradient(rs1, Q1, rs0, Q0);
            var (f0, f1) = bf.Fractions;
            var u0 = Math.Pow(f0, 4) / rs0;
            var u1 = Math.Pow(f1, 4) / rs1;
            var u = u0 + u1;
            var dRp_du = -Math.Pow(u, -2);
            var du_df1 = 4 * u0 / f0;
            var du_df2 = 4 * u1 / f1;

            var du_dR1 = -u0 / rs0 + du_df1 * this.df0_dR0 + du_df2 * this.df1_dR0;
            var du_dR2 = -u1 / rs1 + du_df1 * this.df0_dR1 + du_df2 * this.df1_dR1;
            this.dRp_dR0 = dRp_du * du_dR1;
            this.dRp_dR1 = dRp_du * du_dR2;

            (this.df0_dQ0, this.df1_dQ0) = sr.FlowGradient(rs0, Q0, rs1, Q1);
            (this.df1_dQ1, this.df0_dQ1) = sr.FlowGradient(rs1, Q1, rs0, Q0);
            var du_dQ1 = du_df1 * this.df0_dQ0 + du_df2 * this.df1_dQ0;
            var du_dQ2 = du_df1 * this.df0_dQ1 + du_df2 * this.df1_dQ1;
            this.dRp_dQ0 = dRp_du * du_dQ1;
            this.dRp_dQ1 = dRp_du * du_dQ2;
        }

#pragma warning disable IDE1006 // Naming Styles
        public double dRp_dR0 { get; }
        public double df0_dR0 { get; }
        public double df1_dR0 { get; }
        public double dRp_dR1 { get; }
        public double df0_dR1 { get; }
        public double df1_dR1 { get; }

        public double dRp_dQ0 { get; }
        public double df0_dQ0 { get; }
        public double df1_dQ0 { get; }
        public double dRp_dQ1 { get; }
        public double df0_dQ1 { get; }
        public double df1_dQ1 { get; }

        public double Lp { get; }
        public double L0 { get; }
        public double L1 { get; }
        public Vector3 dLp_dx { get; }
        public Vector3 dL0_dx { get; }
        public Vector3 dL1_dx { get; }

        public Vector3 dR0_dx => this.dL0_dx;
        public Vector3 dR1_dx => this.dL1_dx;
        public Vector3 dRp_dx => this.dLp_dx + this.dRp_dR0 * this.dR0_dx + this.dRp_dR1 * this.dR1_dx;
        public Vector3 df0_dx => this.df0_dR0 * this.dR0_dx + this.df0_dR1 * this.dR1_dx;
        public Vector3 df1_dx => this.df1_dR0 * this.dR0_dx + this.df1_dR1 * this.dR1_dx;
#pragma warning restore IDE1006 // Naming Styles
    }
}
