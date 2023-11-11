using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualBasic;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Structure.Nodes;

/// <summary>
///
/// </summary>
public class HigherSplit : BranchNode, IMobileNode
{
    private Branch[] downstream = null!;
    private Segment[] children = null!;

    private double[] fractions = null!;

    /// <summary>
    ///
    /// </summary>
    public HigherSplit()
    {

    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    public HigherSplit(int n)
    {
        children = new Segment[n];
        downstream = new Branch[n];
        fractions = new double[n];
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="S"></param>
    public HigherSplit(Segment[] S)
    {
        Initialize(S);
    }

    /// <summary>
    /// Takes the array of child segments <paramref name="S"/> without making a copy.
    /// These are currently only created by merges/splits, so the corresponding branches always exist,
    /// and we can do all the housekeeping here for updating the start node references.
    /// </summary>
    /// <param name="S"></param>
    public void Initialize(Segment[] S)
    {
        children = S;
        downstream = new Branch[S.Length];
        fractions = new double[S.Length];
        UpdateDownstream();
        UpdateChildTopology();
    }

    /// <inheritdoc/>
    [NotNull]
    public override Segment? Parent { get; set; } = null;

    /// <inheritdoc/>
    public override Vector3 Position { get; set; } = Vector3.INVALID;

    /// <inheritdoc/>
    public override Segment[] Children => children;

    /// <inheritdoc/>
    public override Branch[] Downstream => downstream;

    /// <inheritdoc/>
    [NotNull]
    public override Branch? Upstream => this.Parent.Branch;

#if !NoEffectiveLength
    /// <inheritdoc/>
    public override double EffectiveLength
    {
        get
        {
            var el = 0.0;
            for (var i = 0; i < downstream.Length; ++i)
            {
                el += downstream[i].EffectiveLength * Math.Pow(fractions[i], 2);
            }
            return el;
        }
    }
#endif

#if !NoPressure
    private double pressure = 0.0;

    /// <inheritdoc/>
    public override double Pressure => pressure;

    /// <inheritdoc/>
    public override void CalculatePressures()
    {
        pressure = this.Upstream.Start.Pressure - this.Upstream.Flow * this.Upstream.Resistance;
        foreach (var d in downstream)
        {
            d.End.CalculatePressures();
        }
    }
#endif

#if !NoDepthPathLength
    private int depth = -1;
    private double pathLength = -1.0;

    /// <inheritdoc/>
    public override double PathLength => pathLength;

    /// <inheritdoc/>
    public override int Depth => depth;

    /// <inheritdoc/>
    public override void CalculatePathLengthsAndDepths()
    {
        depth = this.Upstream.Start.Depth + 1;
        pathLength = this.Upstream.Start.PathLength + this.Upstream.Length;
        foreach (var d in downstream)
        {
            d.End.CalculatePathLengthsAndDepths();
        }
    }

    /// <inheritdoc/>
    public override void CalculatePathLengthsAndOrder()
    {
        pathLength = this.Upstream.Start.PathLength + this.Upstream.Length;
        foreach (var d in downstream)
        {
            d.End.CalculatePathLengthsAndOrder();
        }
        // Inefficient, but not going to be called that often (probably) and still O(d)
        var dMax = downstream.Max(d => d.Depth);
        depth = downstream.All(d => d.Depth == dMax) ? dMax + 1 : dMax;
    }
#endif

    /// <inheritdoc/>
    public override double Flow
    {
        get
        {
            var Q = 0.0;
            for (var i = 0; i < downstream.Length; ++i)
            {
                Q += downstream[i].Flow;
            }
            return Q;
        }
    }

    /// <inheritdoc/>
    public override double ReducedResistance
    {
        get
        {
            var RR = 0.0;
            for (var i = 0; i < downstream.Length; ++i)
            {
                RR += Math.Pow(fractions[i], 4) / downstream[i].ReducedResistance;
            }
            return 1.0 / RR;
        }
    }

    /// <inheritdoc/>
    public override AxialBounds GenerateDownstreamBounds()
    {
        var b = new AxialBounds(downstream[0].GenerateDownstreamBounds());
        for (var i = 1; i < downstream.Length; ++i)
        {
            b.Append(downstream[i].GenerateDownstreamBounds());
        }
        return b;
    }

    /// <inheritdoc/>
    public override AxialBounds GenerateDownstreamBounds(double pad)
    {
        var b = new AxialBounds(downstream[0].GenerateDownstreamBounds(pad));
        for (var i = 1; i < downstream.Length; ++i)
        {
            b.Append(downstream[i].GenerateDownstreamBounds(pad));
        }
        return b;
    }

    /// <inheritdoc/>
    public override void SetChildRadii()
    {
        for (var i = 0; i < downstream.Length; ++i)
        {
            downstream[i].Radius = this.Upstream.Radius * fractions[i];
        }
    }

    /// <inheritdoc/>
    public override void PropagateRadiiDownstream()
    {
        SetChildRadii();
        foreach (var d in downstream)
        {
            d.UpdateRadii();
            d.End.PropagateRadiiDownstream();
        }
    }

    /// <inheritdoc/>
    public override void PropagateRadiiDownstream(double pad)
    {
        SetChildRadii();
        foreach (var d in downstream)
        {
            d.End.PropagateRadiiDownstream();
            d.Radius += pad;
            d.UpdateRadii();
        }
    }

    /// <inheritdoc/>
    public override void PropagateRadiiDownstream(Func<Branch, double> postProcessing)
    {
        SetChildRadii();
        foreach (var d in downstream)
        {
            d.End.PropagateRadiiDownstream();
            d.Radius = postProcessing(d);
            d.UpdateRadii();
        }
    }

    /// <inheritdoc/>
    public override void PropagateLogicalUpstream()
    {
        this.Upstream.PropagateLogicalUpstream();
    }

    private void UpdatePhysicalDerived()
    {
        this.Network.Splitting.Fractions(this, fractions);
    }

    /// <inheritdoc/>
    public override void PropagatePhysicalUpstream()
    {
        UpdatePhysicalDerived();
        this.Upstream.PropagatePhysicalUpstream();
    }

    /// <summary>
    /// Recalculates the lengths of the segments attached to this node.
    /// </summary>
    public void UpdateSegmentLengths()
    {
        foreach (var c in children)
        {
            c.UpdateLength();
        }
        this.Parent.UpdateLength();
    }

    /// <summary>
    /// Updates the branches attached to this node.
    /// </summary>
    public void UpdatePhysicalLocal()
    {
        foreach (var d in downstream)
        {
            d.UpdatePhysicalLocal();
        }
        this.Upstream.UpdatePhysicalLocal();
    }

    /// <summary>
    /// Updates the children, then propagates upstream.
    /// </summary>
    public void UpdatePhysicalGlobalAndPropagate()
    {
        foreach (var d in downstream)
        {
            d.UpdatePhysicalGlobal();
        }
        PropagatePhysicalUpstream();
    }

    /// <inheritdoc/>
    public void UpdatePhysicalAndPropagate()
    {
        UpdateSegmentLengths();
        UpdatePhysicalLocal();
        UpdatePhysicalGlobalAndPropagate();
    }

    /// <inheritdoc/>
    public override void CalculatePhysical()
    {
        foreach (var d in downstream)
        {
            d.UpdateLengths();
            d.UpdatePhysicalLocal();
            d.End.CalculatePhysical();
            d.UpdatePhysicalGlobal();
        }
        UpdatePhysicalDerived();
    }

    /// <summary>
    /// Updates children, then propagates.
    /// </summary>
    public void UpdateLogicalAndPropagate()
    {
        foreach (var d in downstream)
        {
            d.UpdateLogical();
        }
        PropagateLogicalUpstream();
    }

    /// <summary>
    /// Defined so that always less than 1.
    /// </summary>
    public double SplitRatio => fractions.Min() / fractions.Max();

    /// <summary>
    ///
    /// </summary>
    public double[] Fractions => fractions;

    /// <summary>
    ///
    /// </summary>
    /// <param name="weighting"></param>
    /// <returns></returns>
    public Vector3 WeightedMean(Func<Branch, double> weighting)
    {
        var t = weighting(this.Upstream);
        var v = this.Upstream.Start.Position * t;
        foreach (var d in downstream)
        {
            var w = weighting(d);
            t += w;
            v += w * d.End.Position;
        }
        return v / t;
    }
}
