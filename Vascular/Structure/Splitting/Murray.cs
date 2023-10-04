using System;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Splitting
{
    /// <summary>
    /// A constant exponent Murray's law.
    /// </summary>
    [DataContract]
    public class Murray : ISplittingFunction
    {
        [DataMember]
        private double e, e_ni, e_dd, e_dn;

        /// <summary>
        /// 
        /// </summary>
        public double Exponent
        {
            get => e;
            set
            {
                e = value;
                e_ni = -1.0 / e;

                e_dd = -(e + 1) / e;
                e_dn = e - 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double f1, double f2) Fractions(double rs1, double q1, double rs2, double q2)
        {
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            var s = Math.Pow(Math.Pow(c1, e) + Math.Pow(c2, e), e_ni);
            return (c1 * s, c2 * s);
        }

        private (double df1_dc1, double df2_dc1) GroupGradient(double c1, double c2)
        {
            var c1e = Math.Pow(c1, e);
            var c2e = Math.Pow(c2, e);
            var E = c1e + c2e;
            var D = Math.Pow(E, e_ni);
            var F = Math.Pow(E, e_dd) * Math.Pow(c1, e_dn);
            return (D - c1 * F, -c2 * F);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double df1_dq1, double df2_dq1) FlowGradient(double rs1, double q1, double rs2, double q2)
        {
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            (var df1_dc1, var df2_dc1) = GroupGradient(c1, c2);
            var dc1_dq1 = 0.25 * rs1 * Math.Pow(c1, -3);
            return (df1_dc1 * dc1_dq1, df2_dc1 * dc1_dq1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs1"></param>
        /// <param name="q1"></param>
        /// <param name="rs2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public (double df1_drs1, double df2_drs1) ReducedResistanceGradient(double rs1, double q1, double rs2, double q2)
        {
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            (var df1_dc1, var df2_dc1) = GroupGradient(c1, c2);
            var dc1_drs1 = 0.25 * q1 * Math.Pow(c1, -3);
            return (df1_dc1 * dc1_drs1, df2_dc1 * dc1_drs1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="fracs"></param>
        public void Fractions(BranchNode node, double[] fracs)
        {
            var S = 0.0;
            Span<double> c = stackalloc double[fracs.Length];
            for (var i = 0; i < fracs.Length; ++i)
            {
                var di = node.Downstream[i];
                c[i] = Math.Pow(di.Flow * di.ReducedResistance, 0.25);
                S += Math.Pow(c[i], e);
            }
            S = Math.Pow(S, e_ni);
            for (var i = 0; i < fracs.Length; ++i)
            {
                fracs[i] = c[i] * S;
            }
        }

        private void BasicGradient(Span<double> A, Span<double> B, double[,] dfi_dAj)
        {
            Span<double> c = stackalloc double[A.Length];
            var S = 0.0;
            for (var i = 0; i < c.Length; ++i)
            {
                c[i] = Math.Pow(A[i] * B[i], 0.25);
                S += Math.Pow(c[i], e);
            }
            S = Math.Pow(S, e_ni);
            for (var i = 0; i < c.Length; ++i)
            {
                c[i] = c[i] * S;
            }

            // dfi_dAi = fi (1-fi^y) / 4Ai
            // fdi_dAj = -fj fi^y / 4Ai
            for (var i = 0; i < c.Length; ++i)
            {
                for (var j = 0; j < c.Length; ++j)
                {
                    if (i == j)
                    {
                        dfi_dAj[i, j] = c[i] * (1 - Math.Pow(c[i], e)) * 0.25 / A[i];
                    }
                    else
                    {
                        dfi_dAj[i, j] = -c[j] * Math.Pow(c[i], e) * 0.25 / A[i];
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dfi_dRj"></param>
        public void ReducedResistanceGradient(BranchNode node, double[,] dfi_dRj)
        {
            Span<double> R = stackalloc double[node.Downstream.Length];
            Span<double> Q = stackalloc double[node.Downstream.Length];
            for (var i = 0; i < node.Downstream.Length; ++i)
            {
                R[i] = node.Downstream[i].ReducedResistance;
                Q[i] = node.Downstream[i].Flow;
            }
            BasicGradient(R, Q, dfi_dRj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dfi_dQj"></param>
        public void FlowGradient(BranchNode node, double[,] dfi_dQj)
        {
            Span<double> R = stackalloc double[node.Downstream.Length];
            Span<double> Q = stackalloc double[node.Downstream.Length];
            for (var i = 0; i < node.Downstream.Length; ++i)
            {
                R[i] = node.Downstream[i].ReducedResistance;
                Q[i] = node.Downstream[i].Flow;
            }
            BasicGradient(Q, R, dfi_dQj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public (double f1, double f2) Fractions(Bifurcation node)
        {
            var d = node.Downstream;
            return Fractions(d[0].ReducedResistance, d[0].Flow, d[1].ReducedResistance, d[1].Flow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public (double df1_dq1, double df1_dq2, double df2_dq1, double df2_dq2) FlowGradient(Bifurcation node)
        {
            var d = node.Downstream;
            var (df1_dq1, df2_dq1) = FlowGradient(d[0].ReducedResistance, d[0].Flow, d[1].ReducedResistance, d[1].Flow);
            var (df1_dq2, df2_dq2) = FlowGradient(d[1].ReducedResistance, d[1].Flow, d[0].ReducedResistance, d[0].Flow);
            return (df1_dq1, df1_dq2, df2_dq1, df2_dq2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public (double df1_drs1, double df1_drs2, double df2_drs1, double df2_drs2) ReducedResistanceGradient(Bifurcation node)
        {
            var d = node.Downstream;
            var (df1_drs1, df2_drs1) = ReducedResistanceGradient(d[0].ReducedResistance, d[0].Flow, d[1].ReducedResistance, d[1].Flow);
            var (df1_drs2, df2_drs2) = ReducedResistanceGradient(d[1].ReducedResistance, d[1].Flow, d[0].ReducedResistance, d[0].Flow);
            return (df1_drs1, df1_drs2, df2_drs1, df2_drs2);
        }
    }
}
