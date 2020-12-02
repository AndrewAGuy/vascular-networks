using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure.Splitting
{
    [Serializable]
    public class Murray : ISplittingFunction
    {
        private double e;
        private double e_ni;
        private double e_dd;
        private double e_dn;

        public double Exponent
        {
            set
            {
                e = value;
                e_ni = -1.0 / e;

                e_dd = -(e + 1) / e;
                e_dn = e - 1;
            }
        }

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
            var s = Math.Pow(c1e + c2e, e_dd);
            return (c2e * s, -c2 * Math.Pow(c1, e_dn) * s);
        }

        public (double df1_dq1, double df2_dq1) FlowGradient(double rs1, double q1, double rs2, double q2)
        {
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            (var df1_dc1, var df2_dc1) = GroupGradient(c1, c2);
            var dc1_dq1 = 0.25 * rs1 * Math.Pow(c1, -3);
            return (df1_dc1 * dc1_dq1, df2_dc1 * dc1_dq1);
        }

        public (double df1_drs1, double df2_drs1) ReducedResistanceGradient(double rs1, double q1, double rs2, double q2)
        {
            var c1 = Math.Pow(rs1 * q1, 0.25);
            var c2 = Math.Pow(rs2 * q2, 0.25);
            (var df1_dc1, var df2_dc1) = GroupGradient(c1, c2);
            var dc1_drs1 = 0.25 * q1 * Math.Pow(c1, -3);
            return (df1_dc1 * dc1_drs1, df2_dc1 * dc1_drs1);
        }
    }
}
