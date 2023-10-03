using System;
using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// A node with 2 children. Higher order splits are not yet supported as they are not commonly seen in nature, 
    /// although many algorithms implemented would work with them. If needed, approximate using multiple bifurcations.
    /// </summary>
    [DataContract]
    public class Bifurcation : BranchNode, IMobileNode
    {
        /// <summary>
        /// 
        /// </summary>
        public Bifurcation()
        {

        }

        [DataMember]
        private double f0 = 1.0, f1 = 0.0;

        [DataMember]
        private readonly Segment[] children = new Segment[2] { null, null };
        [DataMember]
        private readonly Branch[] downstream = new Branch[2] { null, null };

        /// <inheritdoc/>
        [DataMember]
        public override Segment Parent { get; set; } = null;

        /// <inheritdoc/>
        [DataMember]
        public override Vector3 Position { get; set; } = null;

        /// <inheritdoc/>
        public override Segment[] Children => children;

        /// <inheritdoc/>
        public override Branch[] Downstream => downstream;

        /// <summary>
        /// Construct using a segment view, then pull the branch references in from those.
        /// </summary>
        public override void UpdateDownstream()
        {
            downstream[0] = children[0].Branch;
            downstream[1] = children[1].Branch;
        }

#if !NoEffectiveLength
        /// <inheritdoc/>
        public override double EffectiveLength =>
            downstream[0].EffectiveLength * Math.Pow(f0, 2)
            + downstream[1].EffectiveLength * Math.Pow(f1, 2);
#endif

        /// <inheritdoc/>
        public override double Flow => downstream[0].Flow + downstream[1].Flow;

        /// <inheritdoc/>
        public override double ReducedResistance => 1.0 /
            (Math.Pow(f0, 4.0) / downstream[0].ReducedResistance
            + Math.Pow(f1, 4.0) / downstream[1].ReducedResistance);

        /// <inheritdoc/>
        public override Branch Upstream => this.Parent?.Branch;

#if !NoPressure
        [DataMember]
        private double pressure = 0.0;

        /// <inheritdoc/>
        public override double Pressure => pressure;

        /// <inheritdoc/>
        public override void CalculatePressures()
        {
            pressure = this.Upstream.Start.Pressure - this.Upstream.Flow * this.Upstream.Resistance;
            downstream[0].End.CalculatePressures();
            downstream[1].End.CalculatePressures();
        }
#endif

#if !NoDepthPathLength
        [DataMember]
        private int depth = -1;
        [DataMember]
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
            downstream[0].End.CalculatePathLengthsAndDepths();
            downstream[1].End.CalculatePathLengthsAndDepths();
        }

        /// <inheritdoc/>
        public override void CalculatePathLengthsAndOrder()
        {
            pathLength = this.Upstream.Start.PathLength + this.Upstream.Length;
            downstream[0].End.CalculatePathLengthsAndOrder();
            downstream[1].End.CalculatePathLengthsAndOrder();
            var d0 = downstream[0].Depth;
            var d1 = downstream[1].Depth;
            depth = d0 == d1
                ? d0 + 1
                : Math.Max(d0, d1);
        }
#endif

        /// <inheritdoc/>
        public override AxialBounds GenerateDownstreamBounds()
        {
            return new AxialBounds(downstream[0].GenerateDownstreamBounds())
                .Append(downstream[1].GenerateDownstreamBounds());
        }

        /// <inheritdoc/>
        public override AxialBounds GenerateDownstreamBounds(double pad)
        {
            return new AxialBounds(downstream[0].GenerateDownstreamBounds(pad))
                .Append(downstream[1].GenerateDownstreamBounds(pad));
        }

        /// <inheritdoc/>
        public override void SetChildRadii()
        {
            downstream[0].Radius = this.Upstream.Radius * f0;
            downstream[1].Radius = this.Upstream.Radius * f1;
        }

        /// <inheritdoc/>
        public override void PropagateRadiiDownstream()
        {
            SetChildRadii();
            downstream[0].UpdateRadii();
            downstream[1].UpdateRadii();
            downstream[0].End.PropagateRadiiDownstream();
            downstream[1].End.PropagateRadiiDownstream();
        }

        /// <inheritdoc/>
        public override void PropagateRadiiDownstream(double pad)
        {
            SetChildRadii();
            downstream[0].End.PropagateRadiiDownstream(pad);
            downstream[1].End.PropagateRadiiDownstream(pad);
            downstream[0].Radius += pad;
            downstream[1].Radius += pad;
            downstream[0].UpdateRadii();
            downstream[1].UpdateRadii();
        }

        /// <inheritdoc/>
        public override void PropagateRadiiDownstream(Func<Branch, double> postProcessing)
        {
            SetChildRadii();
            downstream[0].End.PropagateRadiiDownstream(postProcessing);
            downstream[1].End.PropagateRadiiDownstream(postProcessing);
            downstream[0].Radius = postProcessing(downstream[0]);
            downstream[1].Radius = postProcessing(downstream[1]);
            downstream[0].UpdateRadii();
            downstream[1].UpdateRadii();
        }

        /// <inheritdoc/>
        public override void PropagateLogicalUpstream()
        {
            this.Upstream.PropagateLogicalUpstream();
        }

        private void UpdatePhysicalDerived()
        {
            (f0, f1) = this.Network.Splitting.Fractions(
                downstream[0].ReducedResistance, downstream[0].Flow,
                downstream[1].ReducedResistance, downstream[1].Flow);
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
            children[0].UpdateLength();
            children[1].UpdateLength();
            this.Parent.UpdateLength();
        }

        /// <summary>
        /// Updates the branches attached to this node.
        /// </summary>
        public void UpdatePhysicalLocal()
        {
            downstream[0].UpdatePhysicalLocal();
            downstream[1].UpdatePhysicalLocal();
            this.Upstream.UpdatePhysicalLocal();
        }

        /// <summary>
        /// Updates the children, then propagates upstream.
        /// </summary>
        public void UpdatePhysicalGlobalAndPropagate()
        {
            downstream[0].UpdatePhysicalGlobal();
            downstream[1].UpdatePhysicalGlobal();
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
            downstream[0].UpdateLogical();
            downstream[1].UpdateLogical();
            PropagateLogicalUpstream();
        }

        /// <summary>
        /// Outer lengths are those of the sides of the triangle formed by parent and child nodes.
        /// </summary>
        /// <returns></returns>
        public double MinOuterLength()
        {
            var p1 = Vector3.DistanceSquared(this.Upstream.Start.Position, downstream[0].End.Position);
            var p2 = Vector3.DistanceSquared(this.Upstream.Start.Position, downstream[1].End.Position);
            var cc = Vector3.DistanceSquared(downstream[0].End.Position, downstream[1].End.Position);
            return Math.Sqrt(Math.Min(cc, Math.Min(p1, p2)));
        }

        /// <summary>
        /// Inner lengths are those of the star formed by parent and child nodes with this node.
        /// </summary>
        /// <returns></returns>
        public double MinInnerLength()
        {
            var p = Vector3.DistanceSquared(this.Position, this.Upstream.Start.Position);
            var c0 = Vector3.DistanceSquared(this.Position, downstream[0].End.Position);
            var c1 = Vector3.DistanceSquared(this.Position, downstream[1].End.Position);
            return Math.Sqrt(Math.Min(p, Math.Min(c0, c1)));
        }

        /// <summary>
        /// See <see cref="MinOuterLength"/>.
        /// </summary>
        /// <returns></returns>
        public double MaxOuterLength()
        {
            var p1 = Vector3.DistanceSquared(this.Upstream.Start.Position, downstream[0].End.Position);
            var p2 = Vector3.DistanceSquared(this.Upstream.Start.Position, downstream[1].End.Position);
            var cc = Vector3.DistanceSquared(downstream[0].End.Position, downstream[1].End.Position);
            return Math.Sqrt(Math.Max(cc, Math.Max(p1, p2)));
        }

        /// <summary>
        /// See <see cref="MinInnerLength"/>.
        /// </summary>
        /// <returns></returns>
        public double MaxInnerLength()
        {
            var p = Vector3.DistanceSquared(this.Position, this.Upstream.Start.Position);
            var c0 = Vector3.DistanceSquared(this.Position, downstream[0].End.Position);
            var c1 = Vector3.DistanceSquared(this.Position, downstream[1].End.Position);
            return Math.Sqrt(Math.Max(p, Math.Max(c0, c1)));
        }

        /// <summary>
        /// Defined so that always less than 1.
        /// </summary>
        public double BifurcationRatio => f0 > f1 ? f1 / f0 : f0 / f1;

        /// <summary>
        /// 
        /// </summary>
        public (double f0, double f1) Fractions => (f0, f1);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weighting"></param>
        /// <returns></returns>
        public Vector3 WeightedMean(Func<Branch, double> weighting)
        {
            var t = weighting(this.Upstream);
            var v = this.Upstream.Start.Position * t;
            var w = weighting(downstream[0]);
            v += downstream[0].End.Position * w;
            t += w;
            w = weighting(downstream[1]);
            v += downstream[1].End.Position * w;
            t += w;
            return v / t;
        }

        /// <summary>
        /// The index in <see cref="Downstream"/> of <paramref name="branch"/>.
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public int IndexOf(Branch branch)
        {
            return downstream[0] == branch ? 0 : downstream[1] == branch ? 1 : -1;
        }
    }
}
