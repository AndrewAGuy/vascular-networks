using System;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Splitting;

/// <summary>
/// A constant exponent Murray's law.
/// </summary>
public class ConstantMurray : ISplittingFunction
{
    private double e, e_ni;

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
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="exp"></param>
    public ConstantMurray(double exp = 3)
    {
        this.Exponent = exp;
    }

    /// <inheritdoc/>
    public void Fractions(BranchNode node, double[] f)
    {
        var S = 0.0;
        for (var i = 0; i < f.Length; ++i)
        {
            var di = node.Downstream[i];
            f[i] = Math.Pow(di.Flow * di.ReducedResistance, 0.25);
            S += Math.Pow(f[i], e);
        }
        S = Math.Pow(S, e_ni);
        for (var i = 0; i < f.Length; ++i)
        {
            f[i] = f[i] * S;
        }
    }

    /// <inheritdoc/>
    public void Fractions(ReadOnlySpan<double> R, ReadOnlySpan<double> Q, Span<double> f)
    {
        var S = 0.0;
        for (var i = 0; i < f.Length; ++i)
        {
            f[i] = Math.Pow(Q[i] * R[i], 0.25);
            S += Math.Pow(f[i], e);
        }
        S = Math.Pow(S, e_ni);
        for (var i = 0; i < f.Length; ++i)
        {
            f[i] = f[i] * S;
        }
    }

    /// <inheritdoc/>
    public (double f0, double f1) Fractions(Bifurcation node)
    {
        var d = node.Downstream;
        return Fractions(d[0].ReducedResistance, d[0].Flow, d[1].ReducedResistance, d[1].Flow);
    }

    /// <inheritdoc/>
    public (double f0, double f1) Fractions(double rs0, double q0, double rs1, double q1)
    {
        var c0 = Math.Pow(rs0 * q0, 0.25);
        var c1 = Math.Pow(rs1 * q1, 0.25);
        var s = Math.Pow(Math.Pow(c0, e) + Math.Pow(c1, e), e_ni);
        return (c0 * s, c1 * s);
    }

    /// <inheritdoc/>
    public void Gradient(BranchNode node, double[,] dfi_dRj, double[,] dfi_dQj)
    {
        var D = node.Downstream;
        var N = D.Length;
        Span<double> R = stackalloc double[N];
        Span<double> Q = stackalloc double[N];
        for (var i = 0; i < N; ++i)
        {
            R[i] = D[i].ReducedResistance;
            Q[i] = D[i].Flow;
        }

        Span<double> c = stackalloc double[N];
        var S = 0.0;
        for (var i = 0; i < c.Length; ++i)
        {
            c[i] = Math.Pow(R[i] * Q[i], 0.25);
            S += Math.Pow(c[i], e);
        }
        S = Math.Pow(S, e_ni);
        for (var i = 0; i < c.Length; ++i)
        {
            c[i] = c[i] * S;
        }

        // dfi_dAi = fi (1-fi^y) / 4Ai
        // fdi_dAj = -fi fj^y / 4Aj
        for (var i = 0; i < c.Length; ++i)
        {
            for (var j = 0; j < c.Length; ++j)
            {
                if (i == j)
                {
                    dfi_dRj[i, j] = c[i] * (1 - Math.Pow(c[i], e)) * 0.25 / R[i];
                    dfi_dQj[i, j] = c[i] * (1 - Math.Pow(c[i], e)) * 0.25 / Q[i];
                }
                else
                {
                    dfi_dRj[i, j] = -c[i] * Math.Pow(c[j], e) * 0.25 / R[j];
                    dfi_dQj[i, j] = -c[i] * Math.Pow(c[j], e) * 0.25 / Q[j];
                }
            }
        }
    }

    /// <inheritdoc/>
    public (double df0_dR0, double df0_dR1, double df1_dR0, double df1_dR1,
            double df0_dQ0, double df0_dQ1, double df1_dQ0, double df1_dQ1) Gradient(Bifurcation node)
    {
        var R0 = node.Downstream[0].ReducedResistance;
        var R1 = node.Downstream[1].ReducedResistance;
        var Q0 = node.Downstream[0].Flow;
        var Q1 = node.Downstream[1].Flow;
        var (f0, f1) = node.Fractions;
        var f0e = Math.Pow(f0, e);
        var f1e = Math.Pow(f1, e);

        var df0_dR0 = f0 * (1 - f0e) / (4 * R0);
        var df1_dR1 = f1 * (1 - f1e) / (4 * R1);
        var df0_dQ0 = f0 * (1 - f0e) / (4 * Q0);
        var df1_dQ1 = f1 * (1 - f1e) / (4 * Q1);

        var df0_dR1 = -f0 * f1e / (4 * R1);
        var df1_dR0 = -f1 * f0e / (4 * R0);
        var df0_dQ1 = -f0 * f1e / (4 * Q1);
        var df1_dQ0 = -f1 * f0e / (4 * Q0);

        return (df0_dR0, df0_dR1, df1_dR0, df1_dR1, df0_dQ0, df0_dQ1, df1_dQ0, df1_dQ1);
    }
}
