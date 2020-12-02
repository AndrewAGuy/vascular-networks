using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure.Splitting
{
    public abstract class FlowContextualMurray : ISplittingFunction
    {
        public abstract double Exponent(double Q);

        public abstract double ExponentGradient(double Q);

        public (double f1, double f2) Fractions(double rs1, double q1, double rs2, double q2)
        {
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            var e = Exponent(q1 + q2);
            var s = Math.Pow(Math.Pow(c1, e) + Math.Pow(c2, e), -1.0 / e);
            return (c1 * s, c2 * s);
        }

        private (double df1_dc1, double df2_dc1) GroupGradient(double c1, double c2, double e)
        {
            var c1e = Math.Pow(c1, e);
            var c2e = Math.Pow(c2, e);
            var s = Math.Pow(c1e + c2e, -(e + 1) / e);
            return (c2e * s, -c2 * Math.Pow(c1, e - 1) * s);
        }

        public (double df1_dq1, double df2_dq1) FlowGradient(double rs1, double q1, double rs2, double q2)
        {
            // fj = Fj(c1(Q1),c2,e(Q1)) => dfj/dQ1 = dFj/dc1*dc1/dQ1 + dFj/de*de/dQ1  => extra term from exponent
            // Fj(e) = cj*(c1^e+c2^e)^-1/e = cj * u(e)^v(e) => now we have two terms in dFj/de
            // dFj/de = cj * (e^-2 * u(e)^v(e) * ln(u(e)) - e^-1 * u(e)^(-e^-1 - 1) * d/de(c1^e + c2^e))
            // Since e(Q) = e(Q1+Q2) => de/dQ1 = de/dQ

            var Q = q1 + q2;
            var e = Exponent(Q);
            var de_dq1 = ExponentGradient(Q);

            // Same terms as before
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            (var df1_dc1, var df2_dc1) = GroupGradient(c1, c2, e);
            var dc1_dq1 = 0.25 * rs1 * Math.Pow(c1, -3);
            var df1_dq1_c = df1_dc1 * dc1_dq1;
            var df2_dq1_c = df2_dc1 * dc1_dq1;

            // New terms from exponent
            var u1 = Math.Pow(c1, e);
            var u2 = Math.Pow(c2, e);
            var u = u1 + u2;
            var df_de_a = -Math.Pow(u, -1.0 / e) * Math.Log(u) / (e * e);
            var df_de_b = -Math.Pow(u, -(1 - e) / e) * (u1 * Math.Log(u1) + u2 * Math.Log(u2)) / e;
            var df_de = (df_de_a + df_de_b) * de_dq1;
            var df1_dq1_e = df_de * c1;
            var df2_dq1_e = df_de * c2;

            return (df1_dq1_c + df1_dq1_e, df2_dq1_c + df2_dq1_e);
        }

        public (double df1_drs1, double df2_drs1) ReducedResistanceGradient(double rs1, double q1, double rs2, double q2)
        {
            // Exponent constant, so same as other
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            (var df1_dc1, var df2_dc1) = GroupGradient(c1, c2, Exponent(q1 + q2));
            var dc1_drs1 = 0.25 * q1 * Math.Pow(c1, -3);
            return (df1_dc1 * dc1_drs1, df2_dc1 * dc1_drs1);
        }
    }
}
