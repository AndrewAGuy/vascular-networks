using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Optimization.Hierarchical;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric;

/// <summary>
///
/// </summary>
/// <param name="Node"></param>
/// <param name="Gradient"></param>
public record struct GradientEntry(IMobileNode Node, Vector3 Gradient);

/// <summary>
///
/// </summary>
public interface IGradientDescentCost
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public double Cost(Network n);

    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public (double, IEnumerable<GradientEntry>) CostGradient(Network n, Func<IMobileNode, bool>? p);
}

/// <summary>
///
/// </summary>
public enum NonFiniteGradientHandling
{
    /// <summary>
    ///
    /// </summary>
    None,

    /// <summary>
    ///
    /// </summary>
    Filter,

    /// <summary>
    ///
    /// </summary>
    Error
}

/// <summary>
///
/// </summary>
public interface IGradientDescentStepControl
{
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
    double GetStep(List<IMobileNode> N, List<Vector3> G, double c0,
        double aMax, IGradientDescentCost cost, Network network, bool takeStep);
}

/// <summary>
///
/// </summary>
public enum StepMode
{
    /// <summary>
    ///
    /// </summary>
    AllOrNothing,

    /// <summary>
    ///
    /// </summary>
    MaximumPermitted
}

/// <summary>
///
/// </summary>
public interface IGradientDescentStepPredicate
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public bool Permitted(IMobileNode node, Vector3 step);

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public double MaximumPermitted(IMobileNode node, Vector3 step)
    {
        return Permitted(node, step) ? 1 : 0;
    }
}

/// <summary>
///
/// </summary>
public interface IGradientDescentTermination
{
    /// <summary>
    ///
    /// </summary>
    public void Reset();

    /// <summary>
    ///
    /// </summary>
    /// <param name="cost"></param>
    /// <returns></returns>
    public bool Terminate(double cost);
}

/// <summary>
///
/// </summary>
public class GradientDescent
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="Cost"></param>
    /// <param name="Gradients"></param>
    /// <param name="Stride"></param>
    public record IterationInfo(double Cost, IEnumerable<GradientEntry> Gradients, double Stride);

    /// <summary>
    ///
    /// </summary>
    /// <param name="cost"></param>
    public GradientDescent(IGradientDescentCost cost)
    {
        this.cost = cost;
    }

    /// <summary>
    ///
    /// </summary>
    public SoftTopology SoftTopology { get; set; } = new();

    /// <summary>
    ///
    /// </summary>
    public Func<IMobileNode, bool>? RecordingPredicate { get; set; }

    /// <summary>
    ///
    /// </summary>
    public Func<IMobileNode, Vector3, bool>? MovingPredicate { get; set; }

    private readonly IGradientDescentCost cost;

    /// <summary>
    /// The highest fraction of the local geometry that we can step.
    /// When searched over the entire network, the minimum ratio of gradient magnitude to this gives the largest step.
    /// </summary>
    public double StepFraction { get; set; } = 0.125;

    /// <summary>
    ///
    /// </summary>
    public IGradientDescentStepControl StepControl { get; set; } = new ArmijoBacktracker();

    /// <summary>
    ///
    /// </summary>
    public IGradientDescentStepPredicate? StepPredicate { get; set; }

    /// <summary>
    ///
    /// </summary>
    public StepMode StepMode { get; set; } = StepMode.MaximumPermitted;

    /// <summary>
    ///
    /// </summary>
    public NonFiniteGradientHandling NonFiniteGradientHandling { get; set; } = NonFiniteGradientHandling.Filter;

    /// <summary>
    ///
    /// </summary>
    public IGradientDescentTermination? Termination { get; set; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="network"></param>
    /// <param name="iterations"></param>
    /// <param name="runSoftTopologyFirst"></param>
    public void Iterate(Network network, int iterations, bool runSoftTopologyFirst = true)
    {
        if (runSoftTopologyFirst)
        {
            network.Set(true, true, true);
            this.SoftTopology.Update(network);
        }
        this.Termination?.Reset();
        for (var i = 0; i < iterations; ++i)
        {
            if (Iterate(network) != 0)
            {
                return;
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public Action<IterationInfo>? OnStepTaken { get; set; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="network"></param>
    /// <returns></returns>
    public int Iterate(Network network)
    {
        var (c, g) = cost.CostGradient(network, this.RecordingPredicate);
        if (this.Termination is not null)
        {
            if (this.Termination.Terminate(c))
            {
                return 1;
            }
        }

        switch (this.NonFiniteGradientHandling)
        {
            case NonFiniteGradientHandling.None:
            default:
                break;
            case NonFiniteGradientHandling.Filter:
                g = g.Where(p => p.Gradient.IsFinite);
                break;
            case NonFiniteGradientHandling.Error:
                if (g.Any(p => !p.Gradient.IsFinite))
                {
                    return -1;
                }
                break;
        }
        var G = g.Select(p => p.Gradient).ToList();
        if (G.Count == 0)
        {
            return -2;
        }
        var N = g.Select(p => p.Node).ToList();

        var aMax = MaxStride(N, G, this.StepFraction);
        var a = this.StepControl.GetStep(N, G, c, aMax, cost, network, this.StepPredicate is null);
        if (!double.IsFinite(a))
        {
            return -3;
        }
        if (a != 0)
        {
            StepNodes(N, G, a);
            network.Set(false, true, true);
        }
        this.OnStepTaken?.Invoke(new(c, g, a));

        this.SoftTopology.Update(network);
        return 0;
    }

    private static double MaxStride(List<IMobileNode> N, List<Vector3> P, double f = 0.125)
    {
        var sMax = double.PositiveInfinity;
        for (var i = 0; i < N.Count; ++i)
        {
            var s = MaxStride(N[i], P[i]);
            if (!double.IsNaN(s))
            {
                sMax = Math.Min(sMax, s);
            }
        }
        return sMax * f;
    }

    private static double MaxStride(IMobileNode n, Vector3 p)
    {
        var lmin = n.MinSegmentProperty(s => s.Length);
        var lp = p.Length;
        return lmin / lp;
    }

    private void StepNodes(List<IMobileNode> N, List<Vector3> G, double a)
    {
        if (this.StepPredicate is null)
        {
            for (var i = 0; i < N.Count; ++i)
            {
                N[i].Position -= a * G[i];
            }
        }
        else
        {
            if (this.StepMode == StepMode.AllOrNothing)
            {
                for (var i = 0; i < N.Count; ++i)
                {
                    var d = -a * G[i];
                    if (this.StepPredicate.Permitted(N[i], d))
                    {
                        N[i].Position += d;
                    }
                }
            }
            else if (this.StepMode == StepMode.MaximumPermitted)
            {
                for (var i = 0; i < N.Count; ++i)
                {
                    var d0 = -a * G[i];
                    var d = this.StepPredicate.MaximumPermitted(N[i], d0) * d0;
                    N[i].Position += d;
                }
            }
        }
    }
}
