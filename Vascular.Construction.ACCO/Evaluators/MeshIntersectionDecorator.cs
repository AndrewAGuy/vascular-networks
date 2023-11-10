using System;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Evaluators;

/// <summary>
///
/// </summary>
public class MeshIntersectionDecorator : IEvaluator<Branch>
{
    /// <summary>
    ///
    /// </summary>
    public IEvaluator<Branch> InnerEvaluator { get; set; } = new MeanlineEvaluator();

    /// <summary>
    ///
    /// </summary>
    public IAxialBoundsQueryable<TriangleSurfaceTest> Mesh { get; set; } = Array.Empty<TriangleSurfaceTest>().AsQueryable();

    /// <summary>
    ///
    /// </summary>
    public double RadiusFactor { get; set; } = 1;

    /// <summary>
    ///
    /// </summary>
    /// <param name="o"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public Evaluation<Branch> Evaluate(Branch o, Terminal t)
    {
        var eval = this.InnerEvaluator.Evaluate(o, t);
        if (eval.Cost >= 0.0 && eval.Suitable)
        {
            var r = o.Radius * this.RadiusFactor;
            var tst = new TriangleSurfaceTest(o.Start.Position, o.End.Position, t.Position, r);
            // Query returns true if any triangle hits and then terminates, returning true if unsuitable.
            eval.Suitable = !this.Mesh.Query(tst.GetAxialBounds(), TST => tst.TestTriangleRays(TST, out var a, out var b));
        }
        return eval;
    }
}
