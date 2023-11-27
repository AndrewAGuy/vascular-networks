using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Splitting;

internal class UntetheredSplitting// : ISplittingFunction
{
    private readonly Dictionary<BranchNode, double> factors = new();
    public ISplittingFunction Initial { get; set; } = new ConstantMurray();


    public void FlowGradient(BranchNode node, double[,] dfi_dQj)
    {
        throw new System.NotImplementedException();
    }

    public (double df1_dq1, double df1_dq2, double df2_dq1, double df2_dq2) FlowGradient(Bifurcation node)
    {
        throw new System.NotImplementedException();
    }

    public (double df1_dq1, double df2_dq1) FlowGradient(double rs1, double q1, double rs2, double q2)
    {
        throw new System.NotImplementedException();
    }

    private static double Ratio(Branch b) => Math.Pow(b.Flow * b.ReducedResistance, 0.25);

    public void Fractions(BranchNode node, double[] fracs)
    {
        if (!factors.TryGetValue(node, out var f))
        {
            this.Initial.Fractions(node, fracs);
            factors[node] = Ratio(node.Downstream[0]) / fracs[0];
        }
        else
        {
            for (var i = 0; i < fracs.Length; ++i)
            {
                fracs[i] = Ratio(node.Downstream[i]) / f;
            }
        }
    }

    public (double f1, double f2) Fractions(Bifurcation node)
    {
        throw new System.NotImplementedException();
    }

    public (double f1, double f2) Fractions(double rs1, double q1, double rs2, double q2)
    {
        throw new System.NotImplementedException();
    }

    public void ReducedResistanceGradient(BranchNode node, double[,] dfi_dRj)
    {
        throw new System.NotImplementedException();
    }

    public (double df1_drs1, double df1_drs2, double df2_drs1, double df2_drs2) ReducedResistanceGradient(Bifurcation node)
    {
        throw new System.NotImplementedException();
    }

    public (double df1_drs1, double df2_drs1) ReducedResistanceGradient(double rs1, double q1, double rs2, double q2)
    {
        throw new System.NotImplementedException();
    }
}
