using System;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization
{
    /// <summary>
    /// Gradients at a bifurcation depend on the splitting rule used.
    /// </summary>
    public class BifurcationGradients
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bf"></param>
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
            Lp = bf.Upstream.Length;
            L0 = bf.Downstream[0].Length;
            L1 = bf.Downstream[1].Length;
            dLp_dx = Dp / Dp.Length;
            dL0_dx = D0 / D0.Length;
            dL1_dx = D1 / D1.Length;

            var rs0 = bf.Downstream[0].ReducedResistance;
            var rs1 = bf.Downstream[1].ReducedResistance;
            var Q0 = bf.Downstream[0].Flow;
            var Q1 = bf.Downstream[1].Flow;

            (df0_dR0, df1_dR0) = sr.ReducedResistanceGradient(rs0, Q0, rs1, Q1);
            (df1_dR1, df0_dR1) = sr.ReducedResistanceGradient(rs1, Q1, rs0, Q0);
            var (f0, f1) = bf.Fractions;
            var u0 = Math.Pow(f0, 4) / rs0;
            var u1 = Math.Pow(f1, 4) / rs1;
            var u = u0 + u1;
            var dRp_du = -Math.Pow(u, -2);
            var du_df1 = 4 * u0 / f0;
            var du_df2 = 4 * u1 / f1;

            var du_dR1 = -u0 / rs0 + du_df1 * df0_dR0 + du_df2 * df1_dR0;
            var du_dR2 = -u1 / rs1 + du_df1 * df0_dR1 + du_df2 * df1_dR1;
            dRp_dR0 = dRp_du * du_dR1;
            dRp_dR1 = dRp_du * du_dR2;

            (df0_dQ0, df1_dQ0) = sr.FlowGradient(rs0, Q0, rs1, Q1);
            (df1_dQ1, df0_dQ1) = sr.FlowGradient(rs1, Q1, rs0, Q0);
            var du_dQ1 = du_df1 * df0_dQ0 + du_df2 * df1_dQ0;
            var du_dQ2 = du_df1 * df0_dQ1 + du_df2 * df1_dQ1;
            dRp_dQ0 = dRp_du * du_dQ1;
            dRp_dQ1 = dRp_du * du_dQ2;

            dRp_dx = dLp_dx + dRp_dR0 * dL0_dx + dRp_dR1 * dL1_dx;
            df0_dx = df0_dR0 * dL0_dx + df0_dR1 * dL1_dx;
            df1_dx = df1_dR0 * dL0_dx + df1_dR1 * dL1_dx;
        }

        /// <summary>
        /// Derivative of parent reduced resistance with respect to child reduced resistance.
        /// </summary>
        public readonly double dRp_dR0;

        /// <summary>
        /// Derivative of child radius fraction with respect to child reduced resistance.
        /// </summary>
        public readonly double df0_dR0;

        /// <summary>
        /// Derivative of child radius fraction with respect to child reduced resistance.
        /// </summary>
        public readonly double df1_dR0;

        /// <summary>
        /// Derivative of parent reduced resistance with respect to child reduced resistance.
        /// </summary>
        public readonly double dRp_dR1;

        /// <summary>
        /// Derivative of child radius fraction with respect to child reduced resistance.
        /// </summary>
        public readonly double df0_dR1;

        /// <summary>
        /// Derivative of child radius fraction with respect to child reduced resistance.
        /// </summary>
        public readonly double df1_dR1;

        /// <summary>
        /// Derivative of parent reduced resistance with respect to child flow.
        /// </summary>
        public readonly double dRp_dQ0;

        /// <summary>
        /// Derivative of child radius fraction with respect to child flow.
        /// </summary>
        public readonly double df0_dQ0;

        /// <summary>
        /// Derivative of child radius fraction with respect to child flow.
        /// </summary>
        public readonly double df1_dQ0;

        /// <summary>
        /// Derivative of parent reduced resistance with respect to child flow.
        /// </summary>
        public readonly double dRp_dQ1;

        /// <summary>
        /// Derivative of child radius fraction with respect to child flow.
        /// </summary>
        public readonly double df0_dQ1;

        /// <summary>
        /// Derivative of child radius fraction with respect to child flow.
        /// </summary>
        public readonly double df1_dQ1;

        /// <summary>
        /// Parent length.
        /// </summary>
        public readonly double Lp;

        /// <summary>
        /// Child length.
        /// </summary>
        public readonly double L0;

        /// <summary>
        /// Child length.
        /// </summary>
        public readonly double L1;

        /// <summary>
        /// Derivative of parent length with respect to node position.
        /// </summary>
        public readonly Vector3 dLp_dx;

        /// <summary>
        /// Derivative of child length with respect to node position.
        /// </summary>
        public readonly Vector3 dL0_dx;

        /// <summary>
        /// Derivative of child length with respect to node position.
        /// </summary>
        public readonly Vector3 dL1_dx;

        /// <summary>
        /// Derivative of parent reduced resistance with respect to node position.
        /// </summary>
        public readonly Vector3 dRp_dx;

        /// <summary>
        /// Derivative of child radius fraction with respect to node position.
        /// </summary>
        public readonly Vector3 df0_dx;

        /// <summary>
        /// Derivative of child radius fraction with respect to node position.
        /// </summary>
        public readonly Vector3 df1_dx;
    }
}
