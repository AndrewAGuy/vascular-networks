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

    /// <summary>
    ///
    /// </summary>
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="perturbation"></param>
    /// <returns></returns>
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
