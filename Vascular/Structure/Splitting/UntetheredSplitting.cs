using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Splitting;

/// <summary>
/// Allows for splitting factors to become degrees of freedom after initial assignment to a reasonable value
/// </summary>
internal class UntetheredSplitting : ISplittingFunction
{
    private readonly Dictionary<BranchNode, double> factors = new();
    public ISplittingFunction Initial { get; set; } = new ConstantMurray();

    private static double Ratio(Branch b) => Math.Pow(b.Flow * b.ReducedResistance, 0.25);

    public Dictionary<BranchNode, double> Factors => factors;
    public void RestrictToNetwork(Network network, bool trim = true)
    {
        var nodes = network.BranchNodes.ToHashSet();
        var removing = factors.Keys.Where(n => !nodes.Contains(n)).ToList();
        foreach (var r in removing)
        {
            factors.Remove(r);
        }
        if (trim)
        {
            factors.TrimExcess();
        }
    }

    public void Fractions(HigherSplit node, double[] fracs)
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

    public void Fractions(ReadOnlySpan<double> R, ReadOnlySpan<double> Q, Span<double> f)
    {
        this.Initial.Fractions(R, Q, f);
    }

    public (double f0, double f1) Fractions(Bifurcation node)
    {
        if (!factors.TryGetValue(node, out var f))
        {
            var (f0, f1) = this.Initial.Fractions(node);
            factors[node] = Ratio(node.Downstream[0]) / f0;
            return (f0, f1);
        }
        else
        {
            return (Ratio(node.Downstream[0]) / f, Ratio(node.Downstream[1]) / f);
        }
    }

    public (double f0, double f1) Fractions(double rs0, double q0, double rs1, double q1)
    {
        return this.Initial.Fractions(rs0, q0, rs1, q1);
    }

    public void Gradient(HigherSplit node, double[,] dfi_dRj, double[,] dfi_dQj)
    {
        var f = factors[node];
        for (var i = 0; i < node.Downstream.Length; ++i)
        {
            var c = 0.25 * Ratio(node.Downstream[i]) / f;
            dfi_dRj[i, i] = c / node.Downstream[i].ReducedResistance;
            dfi_dQj[i, i] = c / node.Downstream[i].Flow;
            for (var j = 0; j < i; ++j)
            {
                dfi_dRj[i, j] = 0;
                dfi_dQj[i, j] = 0;
            }
            for (var j = i + 1; j < node.Downstream.Length; ++j)
            {
                dfi_dRj[i, j] = 0;
                dfi_dQj[i, j] = 0;
            }
        }
    }

    public (double df0_dR0, double df0_dR1, double df1_dR0, double df1_dR1,
            double df0_dQ0, double df0_dQ1, double df1_dQ0, double df1_dQ1) Gradient(Bifurcation node)
    {
        var f = factors[node];
        var c0 = 0.25 * Ratio(node.Downstream[0]) / f;
        var c1 = 0.25 * Ratio(node.Downstream[1]) / f;
        return (c0 / node.Downstream[0].ReducedResistance, 0, 0, c1 / node.Downstream[1].ReducedResistance,
                c0 / node.Downstream[0].Flow, 0, 0, c1 / node.Downstream[1].Flow);
    }
}
