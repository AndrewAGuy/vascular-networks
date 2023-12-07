using System.Collections.Generic;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric;

/// <summary>
///
/// </summary>
public class GeometricFractionSequence : IGradientDescentStepControl
{
    /// <summary>
    ///
    /// </summary>
    public double InitialFraction { get; set; } = 1;

    /// <summary>
    ///
    /// </summary>
    public int BlockLength { get; set; } = 10;

    /// <summary>
    ///
    /// </summary>
    public double ReductionRatio { get; set; } = 0.75;

    private int iteration = 0;
    private double fraction = 1;

    /// <summary>
    ///
    /// </summary>
    public void Reset()
    {
        iteration = 0;
        fraction = this.InitialFraction;
    }

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
    public double GetStep(List<IMobileNode> N, List<Vector3> G, double c0, double aMax,
        IGradientDescentCost cost, Network network, bool takeStep)
    {
        var f = fraction * aMax;
        ++iteration;
        if (iteration % this.BlockLength == 0)
        {
            fraction *= this.ReductionRatio;
        }

        if (takeStep)
        {
            for (var i = 0; i < N.Count; ++i)
            {
                N[i].Position -= G[i] * f;
            }
            network.Set(false, true);
        }
        return f;
    }
}
