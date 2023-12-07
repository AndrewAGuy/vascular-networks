using System;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric;

/// <summary>
///
/// </summary>
public class MeshIntersectionStepPredicate : IGradientDescentStepPredicate
{
    private readonly IAxialBoundsQueryable<TriangleSurfaceTest> boundary;

    /// <summary>
    ///
    /// </summary>
    /// <param name="boundary"></param>
    public MeshIntersectionStepPredicate(IAxialBoundsQueryable<TriangleSurfaceTest> boundary)
    {
        this.boundary = boundary;
    }

    /// <summary>
    /// Allows conservative testing against meshes for padding
    /// </summary>
    public Func<Segment, double> TestRadius { get; set; } = s => s.Radius * 1.25;

    /// <summary>
    /// Allows some segments to cross the mesh boundary
    /// </summary>
    public Func<Segment, bool> Ignore { get; set; } = n => false;

    /// <inheritdoc/>
    public bool Permitted(IMobileNode node, Vector3 perturbation)
    {
        var position = node.Position + perturbation;

        var start = node.Parent!.Start.Position;
        var radius = this.TestRadius(node.Parent);
        if (!this.Ignore(node.Parent) &&
            (TestRay(start, position - start, radius) <= 1 ||
             this.TestInitial && TestRay(start, node.Position - start, radius) <= 1))
        {
            return false;
        }

        foreach (var c in node.Children)
        {
            start = c.End.Position;
            radius = this.TestRadius(c);
            if (!this.Ignore(c) &&
                (TestRay(start, position - start, radius) <= 1 ||
                 this.TestInitial && TestRay(start, node.Position - start, radius) <= 1))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    public double FractionTolerance { get; set; } = 1e-3;

    /// <summary>
    ///
    /// </summary>
    public double MinimumRayLength
    {
        get => minRayLength;
        set
        {
            minRayLength = Math.Abs(value);
            minRayLengthSquared = Math.Pow(value, 2);
        }
    }

    private double minRayLength = 1e-3;
    private double minRayLengthSquared = 1e-6;

    /// <summary>
    ///
    /// </summary>
    public bool TestShortRaysAsSpheres { get; set; } = true;

    /// <summary>
    ///
    /// </summary>
    public bool TestInitial { get; set; } = true;

    private double TestRay(Vector3 start, Vector3 dir, double radius)
    {
        var l2 = dir.LengthSquared;
        if (l2 <= minRayLengthSquared)
        {
            if (!this.TestShortRaysAsSpheres)
            {
                return 0;
            }
            var d2 = boundary.SquaredDistanceToSurface(start, minRayLength + radius);
            var s = (Math.Sqrt(d2) - radius) / Math.Sqrt(l2);
            return double.IsFinite(s) ? s : 0;
        }
        else
        {
            var f = boundary.RayIntersection(start, dir, radius);
            return double.IsNaN(f) ? 0 : f;
        }
    }

    private double MaximumPermitted(Vector3 start, Vector3 end, Vector3 endPerturbation, double radius)
    {
        // Early termination?
        var d0 = end - start;
        if (this.TestInitial && TestRay(start, d0, radius) <= 1)
        {
            return 0;
        }

        var f = 1.0;
        while (true)
        {
            // For the current step fraction, where do we first hit the boundary along this ray?
            var fDir = d0 + endPerturbation * f;
            var hfFwd = TestRay(start, fDir, radius);
            if (hfFwd <= 0)
            {
                return 0;
            }
            if (hfFwd > 1.0)
            {
                return f;
            }

            f = Math.Min(f, hfFwd);

            // For the ray created by sweeping this first hit location, how far back do we have to go?
            // Prevent a loop of f => hit => sweep back => f by subtracting tolerance
            var hStart = start + hfFwd * d0;
            var hDir = hfFwd * endPerturbation;
            var hfBwd = Math.Min(TestRay(hStart, hDir, radius), 1) * hfFwd - this.FractionTolerance;
            if (hfBwd <= 0)
            {
                return 0;
            }

            f = Math.Min(f, hfBwd);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="perturbation"></param>
    /// <returns></returns>
    public double MaximumPermitted(IMobileNode node, Vector3 perturbation)
    {
        var position = node.Position;
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
