using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure.Splitting
{
    public interface ISplittingFunction
    {
        (double f1, double f2) Fractions(double rs1, double q1, double rs2, double q2);

        (double df1_dq1, double df2_dq1) FlowGradient(double rs1, double q1, double rs2, double q2);

        (double df1_drs1, double df2_drs1) ReducedResistanceGradient(double rs1, double q1, double rs2, double q2);
    }
}
