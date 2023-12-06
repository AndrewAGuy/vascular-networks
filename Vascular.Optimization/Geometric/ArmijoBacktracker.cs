using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric;

/// <summary>
///
/// </summary>
public class ArmijoBacktracker : IGradientDescentStepControl
{
    /// <summary>
    ///
    /// </summary>
    public double ReductionRatio { get; set; } = 0.75;

    /// <summary>
    ///
    /// </summary>
    public double Threshold { get; set; } = 0.5;

    /// <summary>
    ///
    /// </summary>
    public int MaxIterations { get; set; } = 20;

    /// <summary>
    ///
    /// </summary>
    /// <param name="N"></param>
    /// <param name="G"></param>
    /// <param name="c0"></param>
    /// <param name="aMax"></param>
    /// <param name="cost"></param>
    /// <param name="network"></param>
    /// <param name="takeStep"></param>
    /// <returns></returns>
    public double GetStep(List<IMobileNode> N, List<Vector3> G, double c0,
        double aMax, IGradientDescentCost cost, Network network, bool takeStep)
    {
        // Armijo's method uses f(x) - f(x + a*p) >= -a*c * dot(grad(f), p)
        // We just use p = -grad(f)
        var X0 = N.Select(n => n.Position).ToList();
        var a = aMax;
        var t = this.Threshold * Inner(G);
        var s = 0.0;
        for (var j = 0; j < this.MaxIterations; ++j)
        {
            for (var i = 0; i < N.Count; ++i)
            {
                N[i].Position = X0[i] - G[i] * a;
            }
            network.Set(false, true, true);
            var c = cost.Cost(network);

            if (c0 - c >= a * t)
            {
                s = a;
                break;
            }
            a *= this.ReductionRatio;
        }

        if (!takeStep)
        {
            for (var i = 0; i < N.Count; ++i)
            {
                N[i].Position = X0[i];
            }
            network.Set(false, true, true);
            return s;
        }
        return 0;
    }

    private static double Inner(List<Vector3> g)
    {
        var total = 0.0;
        for (var i = 0; i < g.Count; ++i)
        {
            total += g[i].LengthSquared;
        }
        return total;
    }
}
