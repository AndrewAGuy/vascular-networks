using System;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Hierarchical;

/// <summary>
///
/// </summary>
public class SplittingGradients
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="hs"></param>
    public SplittingGradients(HigherSplit hs)
    {
        var sr = hs.Network.Splitting;
        var bx = hs.Position;
        var px = hs.Parent.Start.Position;
        var N = hs.Downstream.Length;

        var Dp = bx - px;
        Lp = hs.Upstream.Length;
        dLp_dx = Dp / Dp.Length;

        Li = new double[N];
        dLi_dx = new Vector3[N];
        for (var i = 0; i < N; ++i)
        {
            var Di = bx - hs.Children[i].End.Position;
            Li[i] = hs.Downstream[i].Length;
            dLi_dx[i] = Di / Di.Length;
        }

        dfi_dRj = new double[N, N];
        dfi_dQj = new double[N, N];
        sr.Gradient(hs, dfi_dRj, dfi_dQj);
        //sr.ReducedResistanceGradient(hs, dfi_dRj);
        //sr.FlowGradient(hs, dfi_dQj);

        var u = 0.0;
        Span<double> du_dfi = stackalloc double[N];
        Span<double> ui = stackalloc double[N];
        for (var i = 0; i < N; ++i)
        {
            var fi = hs.Fractions[i];
            var rsi = hs.Downstream[i].ReducedResistance;
            ui[i] = Math.Pow(fi, 4) / rsi;
            u += ui[i];
            du_dfi[i] = 4 * ui[i] / fi;
        }
        var dRp_du = -Math.Pow(u, -2);

        dRp_dRi = new double[N];
        dRp_dQi = new double[N];
        for (var i = 0; i < N; ++i)
        {
            var rsi = hs.Downstream[i].ReducedResistance;
            var du_dRi = -ui[i] / rsi;
            var du_dQi = 0.0;
            for (var j = 0; j < N; ++j)
            {
                du_dRi += du_dfi[j] * dfi_dRj[j, i];
                du_dQi += du_dfi[j] * dfi_dQj[j, i];
            }
            dRp_dRi[i] = dRp_du * du_dRi;
            dRp_dQi[i] = dRp_du * du_dQi;
        }

        dfi_dx = new Vector3[N];
        dRp_dx = dLp_dx;
        for (var i = 0; i < N; ++i)
        {
            dRp_dx += dRp_dRi[i] * dLi_dx[i];
            dfi_dx[i] = new();
            for (var j = 0; j < N; ++j)
            {
                dfi_dx[i] += dfi_dRj[i, j] * dLi_dx[j];
            }
        }
    }

    /// <summary>
    /// Derivative of parent reduced resistance with respect to child reduced resistance.
    /// </summary>
    public readonly double[] dRp_dRi;

    /// <summary>
    /// Derivative of child radius fraction with respect to child reduced resistance.
    /// </summary>
    public readonly double[,] dfi_dRj;

    /// <summary>
    /// Derivative of parent reduced resistance with respect to child flow.
    /// </summary>
    public readonly double[] dRp_dQi;

    /// <summary>
    /// Derivative of child radius fraction with respect to child flow.
    /// </summary>
    public readonly double[,] dfi_dQj;

    /// <summary>
    /// Parent length.
    /// </summary>
    public readonly double Lp;

    /// <summary>
    /// Child length.
    /// </summary>
    public readonly double[] Li;

    /// <summary>
    /// Derivative of parent length with respect to node position.
    /// </summary>
    public readonly Vector3 dLp_dx;

    /// <summary>
    /// Derivative of child length with respect to node position.
    /// </summary>
    public readonly Vector3[] dLi_dx;

    /// <summary>
    /// Derivative of parent reduced resistance with respect to node position.
    /// </summary>
    public readonly Vector3 dRp_dx;

    /// <summary>
    /// Derivative of child radius fraction with respect to node position.
    /// </summary>
    public readonly Vector3[] dfi_dx;
}
