using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Vascular.Structure.Splitting
{
    /// <summary>
    /// Murray's law where the exponent depends on flow rate.
    /// </summary>
    [DataContract]
    [KnownType(typeof(ClampedMurray))]
    [KnownType(typeof(ExponentialMurray))]
    public abstract class FlowContextualMurray : ISplittingFunction, IArbitrarySplittingFunction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public abstract double Exponent(double Q);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public abstract double ExponentGradient(double Q);

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
            var e = Exponent(q1 + q2);
            var s = Math.Pow(Math.Pow(c1, e) + Math.Pow(c2, e), -1.0 / e);
            return (c1 * s, c2 * s);
        }

        private static (double df1_dc1, double df2_dc1) GroupGradient(double c1, double c2, double e)
        {
            var c1e = Math.Pow(c1, e);
            var c2e = Math.Pow(c2, e);
            var E = c1e + c2e;
            var D = Math.Pow(E, -1.0 / e);
            var F = Math.Pow(E, (-1 - e) / e) * Math.Pow(c1, e - 1);
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
            // fj = Fj(c1(Q1),c2,e(Q1)) => dfj/dQ1 = dFj/dc1*dc1/dQ1 + dFj/de*de/dQ1  => extra term from exponent
            // Fj(e) = cj*(c1^e+c2^e)^-1/e = cj * u(e)^v(e) => now we have two terms in dFj/de
            // dFj/de = cj * (e^-2 * u(e)^v(e) * ln(u(e)) - e^-1 * u(e)^(-e^-1 - 1) * d/de(c1^e + c2^e))
            // Since e(Q) = e(Q1+Q2) => de/dQ1 = de/dQ

            var Q = q1 + q2;
            var e = Exponent(Q);
            var de_dq1 = ExponentGradient(Q);

            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            // Same terms as context-free, but cache the terms for later rather than use group gradient method
            var c1e = Math.Pow(c1, e);
            var c2e = Math.Pow(c2, e);
            var E = c1e + c2e;
            var D = Math.Pow(E, -1.0 / e);
            var F = Math.Pow(E, (-1 - e) / e);
            var G = F * Math.Pow(c1, e - 1);
            (var df1_dc1, var df2_dc1) = (D - c1 * G, -c2 * G);
            var dc1_dq1 = 0.25 * rs1 * Math.Pow(c1, -3);
            var df1_dq1_c = df1_dc1 * dc1_dq1;
            var df2_dq1_c = df2_dc1 * dc1_dq1;

            // New terms from exponent
            var df_de_a = D * Math.Log(E) / (e * e);
            var df_de_b = F * (c1e * Math.Log(c1e) + c2e * Math.Log(c2e)) / e;
            var df_de = (df_de_a - df_de_b) * de_dq1;
            var df1_dq1_e = df_de * c1;
            var df2_dq1_e = df_de * c2;

            return (df1_dq1_c + df1_dq1_e, df2_dq1_c + df2_dq1_e);
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
            // Exponent constant, so same as other
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            (var df1_dc1, var df2_dc1) = GroupGradient(c1, c2, Exponent(q1 + q2));
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
            var e = Exponent(node.Flow);
            var S = 0.0;
            Span<double> c = stackalloc double[fracs.Length];
            for (var i = 0; i < fracs.Length; ++i)
            {
                var di = node.Downstream[i];
                c[i] = Math.Pow(di.Flow * di.ReducedResistance, 0.25);
                S += Math.Pow(c[i], e);
            }
            S = Math.Pow(S, -1.0 / e);
            for (var i = 0; i < fracs.Length; ++i)
            {
                fracs[i] = c[i] * S;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dfi_dRj"></param>
        public void ReducedResistanceGradient(BranchNode node, double[,] dfi_dRj)
        {
            var e = Exponent(node.Flow);
            Span<double> R = stackalloc double[node.Downstream.Length];
            Span<double> Q = stackalloc double[node.Downstream.Length];
            for (var i = 0; i < node.Downstream.Length; ++i)
            {
                R[i] = node.Downstream[i].ReducedResistance;
                Q[i] = node.Downstream[i].Flow;
            }
            BasicGradient(R, Q, dfi_dRj, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dfi_dQj"></param>
        public void FlowGradient(BranchNode node, double[,] dfi_dQj)
        {
            // Start the same, then add in exponent terms
            var e = Exponent(node.Flow);
            Span<double> R = stackalloc double[node.Downstream.Length];
            Span<double> Q = stackalloc double[node.Downstream.Length];
            for (var i = 0; i < node.Downstream.Length; ++i)
            {
                R[i] = node.Downstream[i].ReducedResistance;
                Q[i] = node.Downstream[i].Flow;
            }
            BasicGradient(Q, R, dfi_dQj, e);

            var de_dq = ExponentGradient(node.Flow);
            ExponentGradient(Q, R, dfi_dQj, e, de_dq);
        }

        private void ExponentGradient(Span<double> Q, Span<double> R, double[,] dfi_dQj, double e, double de_dq)
        {
            // TODO: make split version to save time recomputing c, or make it allocated in the caller
            Span<double> c = stackalloc double[Q.Length];
            var a = 0.0;
            var da_dy = 0.0;
            for (var i = 0; i < c.Length; ++i)
            {
                c[i] = Math.Pow(Q[i] * R[i], 0.25);
                var cy = Math.Pow(c[i], e);
                a += cy;
                da_dy += cy * Math.Log(c[i]);
            }
            var b = -1.0 / e;
            var db_dy = 1.0 / (e * e);

            // We add in terms of ci * dF/dy dy/dQj to dfi_dqj, where dy/dQj = dy/dQ
            // F = a^b, where a = sum(ci^y) and b = -1/y
            // dF/dy = b a^(b-1) da/dy + a^b log a db/dy
            // da/dy = sum(ci^y log ci)
            // db/dy = 1/y^2
            var dF_dy = b * Math.Pow(a, b - 1) * da_dy + Math.Pow(a, b) * Math.Log(a) * db_dy;
            var dF_dQj = de_dq * dF_dy;

            for (var i = 0; i < Q.Length; ++i)
            {
                for (var j = 0; j < Q.Length; ++j)
                {
                    dfi_dQj[i, j] += c[i] * dF_dQj;
                }
            }
        }

        private void BasicGradient(Span<double> A, Span<double> B, double[,] dfi_dAj, double e)
        {
            Span<double> c = stackalloc double[A.Length];
            var S = 0.0;
            for (var i = 0; i < c.Length; ++i)
            {
                c[i] = Math.Pow(A[i] * B[i], 0.25);
                S += Math.Pow(c[i], e);
            }
            S = Math.Pow(S, -1.0 / e);
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
    }
}
