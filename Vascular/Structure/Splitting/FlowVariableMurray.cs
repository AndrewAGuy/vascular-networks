using System;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Splitting;

/// <summary>
///
/// </summary>
public abstract class FlowVariableMurray : ISplittingFunction
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
    /// <param name="node"></param>
    /// <param name="f"></param>
    public void Fractions(HigherSplit node, double[] f)
    {
        var e = Exponent(node.Flow);
        var S = 0.0;
        for (var i = 0; i < f.Length; ++i)
        {
            var di = node.Downstream[i];
            f[i] = Math.Pow(di.Flow * di.ReducedResistance, 0.25);
            S += Math.Pow(f[i], e);
        }
        S = Math.Pow(S, -1.0 / e);
        for (var i = 0; i < f.Length; ++i)
        {
            f[i] = f[i] * S;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="R"></param>
    /// <param name="Q"></param>
    /// <param name="f"></param>
    public void Fractions(ReadOnlySpan<double> R, ReadOnlySpan<double> Q, Span<double> f)
    {
        var q = 0.0;
        for (var i = 0; i < f.Length; ++i)
        {
            q += Q[i];
        }
        var e = Exponent(q);

        var S = 0.0;
        for (var i = 0; i < f.Length; ++i)
        {
            f[i] = Math.Pow(Q[i] * R[i], 0.25);
            S += Math.Pow(f[i], e);
        }
        S = Math.Pow(S, -1.0 / e);
        for (var i = 0; i < f.Length; ++i)
        {
            f[i] = f[i] * S;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public (double f0, double f1) Fractions(Bifurcation node)
    {
        var d = node.Downstream;
        return Fractions(d[0].ReducedResistance, d[0].Flow, d[1].ReducedResistance, d[1].Flow);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="rs0"></param>
    /// <param name="q0"></param>
    /// <param name="rs1"></param>
    /// <param name="q1"></param>
    /// <returns></returns>
    public (double f0, double f1) Fractions(double rs0, double q0, double rs1, double q1)
    {
        var c0 = Math.Pow(rs0 * q0, 0.25);
        var c1 = Math.Pow(rs1 * q1, 0.25);
        var e = Exponent(q0 + q1);
        var s = Math.Pow(Math.Pow(c0, e) + Math.Pow(c1, e), -1.0 / e);
        return (c0 * s, c1 * s);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="dfi_dRj"></param>
    /// <param name="dfi_dQj"></param>
    public void Gradient(HigherSplit node, double[,] dfi_dRj, double[,] dfi_dQj)
    {
        var D = node.Downstream;
        var f = node.Fractions;
        var N = D.Length;
        Span<double> R = stackalloc double[N];
        Span<double> Q = stackalloc double[N];
        for (var i = 0; i < N; ++i)
        {
            R[i] = D[i].ReducedResistance;
            Q[i] = D[i].Flow;
        }
        var e = Exponent(node.Flow);
        var de_dq = ExponentGradient(node.Flow);

        Span<double> c = stackalloc double[N];
        var a = 0.0;
        var da_de = 0.0;
        for (var i = 0; i < c.Length; ++i)
        {
            c[i] = Math.Pow(R[i] * Q[i], 0.25);
            var ce = Math.Pow(c[i], e);
            a += ce;
            da_de += ce * Math.Log(c[i]);
        }
        var b = -1.0 / e;
        var db_de = 1.0 / (e * e);
        var dF_de = b * Math.Pow(a, b - 1) * da_de + Math.Pow(a, b) * Math.Log(a) * db_de;
        var dF_dQ = de_dq * dF_de;

        for (var i = 0; i < c.Length; ++i)
        {
            var dQ = c[i] * dF_dQ;
            for (var j = 0; j < c.Length; ++j)
            {
                if (i == j)
                {
                    dfi_dRj[i, j] = f[i] * (1 - Math.Pow(f[i], e)) * 0.25 / R[i];
                    dfi_dQj[i, j] = f[i] * (1 - Math.Pow(f[i], e)) * 0.25 / Q[i] + dQ;
                }
                else
                {
                    dfi_dRj[i, j] = -f[i] * Math.Pow(f[j], e) * 0.25 / R[j];
                    dfi_dQj[i, j] = -f[i] * Math.Pow(f[j], e) * 0.25 / Q[j] + dQ;
                }
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public (double df0_dR0, double df0_dR1, double df1_dR0, double df1_dR1,
            double df0_dQ0, double df0_dQ1, double df1_dQ0, double df1_dQ1) Gradient(Bifurcation node)
    {
        var R0 = node.Downstream[0].ReducedResistance;
        var R1 = node.Downstream[1].ReducedResistance;
        var Q0 = node.Downstream[0].Flow;
        var Q1 = node.Downstream[1].Flow;
        var Q = Q0 + Q1;
        var e = Exponent(Q);
        var de_dq = ExponentGradient(Q);

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

        // We add in terms of ci * dF/dy dy/dQj to dfi_dqj, where dy/dQj = dy/dQ
        // F = a^b, where a = sum(ci^y) and b = -1/y
        // dF/dy = b a^(b-1) da/dy + a^b log a db/dy
        // da/dy = sum(ci^y log ci)
        // db/dy = 1/y^2

        var c0 = Math.Pow(R0 * Q0, 0.25);
        var c1 = Math.Pow(R1 * Q1, 0.25);
        var c0e = Math.Pow(c0, e);
        var c1e = Math.Pow(c1, e);
        var a = c0e + c1e;
        var da_de = c0e * Math.Log(c0) + c1e * Math.Log(c1);
        var b = -1.0 / e;
        var db_de = 1.0 / (e * e);

        var dF_de = b * Math.Pow(a, b - 1) * da_de + Math.Pow(a, b) * Math.Log(a) * db_de;
        var dF_dQ = de_dq * dF_de;

        df0_dQ0 += c0 * dF_dQ;
        df0_dQ1 += c0 * dF_dQ;
        df1_dQ0 += c1 * dF_dQ;
        df1_dQ1 += c1 * dF_dQ;

        return (df0_dR0, df0_dR1, df1_dR0, df1_dR1, df0_dQ0, df0_dQ1, df1_dQ0, df1_dQ1);
    }
}
