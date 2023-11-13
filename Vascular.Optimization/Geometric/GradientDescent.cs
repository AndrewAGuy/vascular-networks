using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Optimization.Hierarchical;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric;

public interface IGradientDescentCost
{
    public double Cost(Network n);
    public (double, IEnumerable<GradientEntry>) CostGradient(Network n, Func<IMobileNode, bool>? p);
}

public record struct GradientEntry(IMobileNode Node, Vector3 Gradient);

class HierarchicalWrapper : IGradientDescentCost
{
    private readonly HierarchicalCost cost;

    public HierarchicalWrapper(HierarchicalCost cost)
    {
        this.cost = cost;
    }

    public double Cost(Network n)
    {
        return cost.SetCost(n);
    }

    public (double, IEnumerable<GradientEntry>) CostGradient(Network n, Func<IMobileNode, bool>? p)
    {
        cost.SetCache(n);
        if (p is not null)
        {
            IEnumerable<GradientEntry> gradients()
            {
                foreach (var node in n.MobileNodes)
                {
                    if (p(node))
                    {
                        yield return new(node, cost.PositionGradient(node));
                    }
                }
            }
            return (cost.Cost, gradients());
        }
        else
        {
            IEnumerable<GradientEntry> gradients()
            {
                foreach (var node in n.MobileNodes)
                {
                    yield return new(node, cost.PositionGradient(node));
                }
            }
            return (cost.Cost, gradients());
        }
    }
}

public interface IGradientDescentStepManager
{
    public double Step { get; }
    public void Update(double cost);
}

public class GradientDescent
{
    public record IterationInfo(double Cost, IEnumerable<GradientEntry> Gradients, double Stride);

    public GradientDescent(IGradientDescentCost cost)
    {
        this.cost = cost;
    }

    // Want to expose the following work sequence:
    //  Get cost, gradients (possible pre-filtered)
    //  - process gradients
    //      - is it enough to allow (n, v) => (n, w)
    //      - or do we want to be able to add in new (n, v) pairs or target specific nodes?
    //
    //  Step control (prevent instability, encourage convergence)
    //      - Max step size can use knowledge of local geometry
    //      - Reducing step size over time?
    //      - Armijo backtracking needs to be able to evaluate cost, possibly gradient if using Wolfe conditions
    //          - Also needs way to store x_k and set x_k + a*p_k.
    //          - Either dicts or lists in same order
    //  Termination on cost differences?
    //  Update soft topology

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

    // public Func<List<IMobileNode>, List<Vector3>, List<Vector3>> SearchDirection { get; set; }
    //     = (N, G) => G.Select(g => -g).ToList();

    // public Func<List<IMobileNode>, List<Vector3>, double> MaximumStep { get; set; }
    //     = (N, P) => MaxStrideByNodes(N, P);

    //private readonly List<IGradientDescentCost> costs = new();

    private readonly IGradientDescentCost cost;

    public double StepFraction { get; set; } = 0.125;
    public IGradientDescentStepControl StepControl { get; set; } = new ArmijoBacktracker();

    public IGradientDescentStepPredicate? StepPredicate { get; set; }
    public StepMode StepMode { get; set; } = StepMode.MaximumPermitted;

    //public Func<Network,double> MaximumStep { get; set; }

    public NonFiniteGradientHandling NonFiniteGradientHandling { get; set; } = NonFiniteGradientHandling.Filter;

    public Func<double, bool>? TerminationPredicate { get; set; }

    public void Iterate(Network network, int iterations)
    {
        for (var i = 0; i < iterations; ++i)
        {
            if (Iterate(network) != 0)
            {
                return;
            }
        }
    }

    public Action<IterationInfo>? OnStepTaken { get; set; }

    public int Iterate(Network network)
    {
        var (c, g) = cost.CostGradient(network, this.RecordingPredicate);
        if (this.TerminationPredicate is not null)
        {
            if (this.TerminationPredicate(c))
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
        var a = this.StepControl.GetStep(N, G, c, aMax, cost, network);
        if (!double.IsFinite(a))
        {
            return -3;
        }
        StepNodes(N, G, a);
        network.Set(true, true, true);
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
                    var d = -this.StepPredicate.MaximumPermitted(N[i], d0) * G[i];
                    N[i].Position += d;
                }
            }
        }
    }
}

public enum NonFiniteGradientHandling
{
    None,
    Filter,
    Error
}

public interface IGradientDescentStepControl
{
    double GetStep(List<IMobileNode> N, List<Vector3> G, double c0,
        double aMax, IGradientDescentCost cost, Network network);
}

public class ArmijoBacktracker : IGradientDescentStepControl
{
    public double ReductionRatio { get; set; } = 0.75;

    public double Threshold { get; set; } = 0.5;

    public int MaxIterations { get; set; } = 20;

    public double GetStep(List<IMobileNode> N, List<Vector3> G, double c0,
        double aMax, IGradientDescentCost cost, Network network)
    {
        // Armijo's method uses f(x) - f(x + a*p) >= -a*c * dot(grad(f), p)
        // We just use p = -grad(f)
        var X0 = N.Select(n => n.Position).ToList();
        var a = aMax;
        var t = this.Threshold * Inner(G);
        for (var j = 0; j < this.MaxIterations; ++j)
        {
            for (var i = 0; i < N.Count; ++i)
            {
                N[i].Position = X0[i] - G[i] * a;
            }
            network.Set(true, true, true);
            var c = cost.Cost(network);

            if (c0 - c >= a * t)
            {
                return a;
            }
            a *= this.ReductionRatio;
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

public enum StepMode
{
    AllOrNothing,
    MaximumPermitted
}

public interface IGradientDescentStepPredicate
{
    public bool Permitted(IMobileNode node, Vector3 step);

    public double MaximumPermitted(IMobileNode node, Vector3 step)
    {
        return Permitted(node, step) ? 1 : 0;
    }
}

public class MeshIntersectionPreventer : IGradientDescentStepPredicate
{
    private readonly IAxialBoundsQueryable<TriangleSurfaceTest> boundary;

    public MeshIntersectionPreventer(IAxialBoundsQueryable<TriangleSurfaceTest> boundary)
    {
        this.boundary = boundary;
    }

    public Func<Segment, double> TestRadius { get; set; } = s => s.Radius * 1.25;
    public Func<Segment, bool> Ignore { get; set; } = n => false;

    public bool Permitted(IMobileNode node, Vector3 perturbation)
    {
        var position = node.Position + perturbation;

        var start = node.Parent!.Start.Position;
        if (!this.Ignore(node.Parent) &&
            boundary.RayIntersects(start, position - start, this.TestRadius(node.Parent)))
        {
            return false;
        }

        foreach (var c in node.Children)
        {
            start = c.End.Position;
            if (!this.Ignore(c) &&
                boundary.RayIntersects(start, position - start, this.TestRadius(c)))
            {
                return false;
            }
        }

        return true;
    }

    public double FractionTolerance { get; set; } = 1e-3;

    private double MaximumPermitted(Vector3 start, Vector3 end, Vector3 endPerturbation, double radius)
    {
        var f = 1.0;
        var d0 = end - start;
        while (true)
        {
            // For the current step fraction, where do we first hit the boundary along this ray?
            var fDir = d0 + endPerturbation * f;
            var hf = boundary.RayIntersection(start, fDir, radius);
            if (hf > 1.0)
            {
                return f;
            }

            // For the ray created by sweeping this first hit location, how far back do we have to go?
            // Prevent a loop of f => hit => sweep back => f by subtracting tolerance
            var hStart = start + hf * d0;
            var hDir = hf * endPerturbation;
            hf = boundary.RayIntersection(hStart, hDir, radius) - this.FractionTolerance;
            if (hf <= 0)
            {
                return 0;
            }

            f = Math.Min(f, hf);
        }
    }

    public double MaximumPermitted(IMobileNode node, Vector3 perturbation)
    {
        var position = node.Position + perturbation;
        var minFraction = 1.0;

        var start = node.Parent!.Start.Position;
        if (!this.Ignore(node.Parent))
        {
            var f = MaximumPermitted(start, position, perturbation, this.TestRadius(node.Parent));
            minFraction = Math.Min(minFraction, f);
        }

        foreach (var c in node.Children)
        {
            start = c.End.Position;
            if (!this.Ignore(c))
            {
                var f = MaximumPermitted(start, position, perturbation, this.TestRadius(c));
                minFraction = Math.Min(minFraction, f);
            }
        }

        return minFraction;
    }
}
