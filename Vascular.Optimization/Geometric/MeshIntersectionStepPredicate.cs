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
            (boundary.RayIntersects(start, position - start, radius) ||
             boundary.RayIntersects(start, node.Position - start, radius)))
        {
            return false;
        }

        foreach (var c in node.Children)
        {
            start = c.End.Position;
            radius = this.TestRadius(c);
            if (!this.Ignore(c) &&
                (boundary.RayIntersects(start, position - start, radius) ||
                 boundary.RayIntersects(start, node.Position - start, radius)))
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

    private double MaximumPermitted(Vector3 start, Vector3 end, Vector3 endPerturbation, double radius)
    {
        // Early termination?
        var d0 = end - start;
        if (boundary.RayIntersects(start, d0, radius))
        {
            return 0;
        }

        var f = 1.0;
        while (true)
        {
            // For the current step fraction, where do we first hit the boundary along this ray?
            var fDir = d0 + endPerturbation * f;
            if (fDir.LengthSquared <= minRayLengthSquared)
            {
                return 0;
            }

            var hfFwd = boundary.RayIntersection(start, fDir, radius);
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
            if (hDir.LengthSquared <= minRayLengthSquared)
            {
                return 0;
            }
            var hfBwd = boundary.RayIntersection(hStart, hDir, radius) * hfFwd - this.FractionTolerance;
            if (hfBwd <= 0)
            {
                return 0;
            }
            // else if (hfBwd > 1)
            // {
            //     return hfFwd;
            // }

            f = Math.Min(f, hfBwd);
            // if (f <= this.FractionTolerance)
            // {
            //     return 0;
            // }
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
