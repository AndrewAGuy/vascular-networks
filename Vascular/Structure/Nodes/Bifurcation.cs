using System;
using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Structure.Nodes
{
    [DataContract]
    public class Bifurcation : BranchNode, IMobileNode
    {
        public Bifurcation()
        {

        }

        [DataMember]
        private double f0 = 1.0, f1 = 0.0;

        [DataMember]
        private readonly Segment[] children = new Segment[2] { null, null };
        [DataMember]
        private readonly Branch[] downstream = new Branch[2] { null, null };

        [DataMember]
        public override Segment Parent { get; set; } = null;

        [DataMember]
        public override Vector3 Position { get; set; } = null;

        public override Segment[] Children => children;

        public override Branch[] Downstream => downstream;

        public void UpdateDownstream()
        {
            downstream[0] = children[0].Branch;
            downstream[1] = children[1].Branch;
        }

#if !NoEffectiveLength
        public override double EffectiveLength => 
            downstream[0].EffectiveLength * Math.Pow(f0, 2) 
            + downstream[1].EffectiveLength * Math.Pow(f1, 2);
#endif

        public override double Flow => downstream[0].Flow + downstream[1].Flow;

        public override double ReducedResistance => 1.0 / 
            (Math.Pow(f0, 4.0) / downstream[0].ReducedResistance 
            + Math.Pow(f1, 4.0) / downstream[1].ReducedResistance);

        public override Branch Upstream => this.Parent?.Branch;

#if !NoPressure
        [DataMember]
        private double pressure = 0.0;

        public override double Pressure => pressure;

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

        public override double PathLength => pathLength;

        public override int Depth => depth;

        public override void CalculatePathLengthsAndDepths()
        {
            depth = this.Upstream.Start.Depth + 1;
            pathLength = this.Upstream.Start.PathLength + this.Upstream.Length;
            downstream[0].End.CalculatePathLengthsAndDepths();
            downstream[1].End.CalculatePathLengthsAndDepths();
        }
#endif

        public override AxialBounds GenerateDownstreamBounds()
        {
            return new AxialBounds(downstream[0].GenerateDownstreamBounds())
                .Append(downstream[1].GenerateDownstreamBounds());
        }

        public override AxialBounds GenerateDownstreamBounds(double pad)
        {
            return new AxialBounds(downstream[0].GenerateDownstreamBounds(pad))
                .Append(downstream[1].GenerateDownstreamBounds(pad));
        }

        public override void SetChildRadii()
        {
            downstream[0].Radius = this.Upstream.Radius * f0;
            downstream[1].Radius = this.Upstream.Radius * f1;
        }

        public override void PropagateRadiiDownstream()
        {
            SetChildRadii();
            downstream[0].UpdateRadii();
            downstream[1].UpdateRadii();
            downstream[0].End.PropagateRadiiDownstream();
            downstream[1].End.PropagateRadiiDownstream();
        }

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

        public override void PropagatePhysicalUpstream()
        {
            UpdatePhysicalDerived();
            this.Upstream.PropagatePhysicalUpstream();
        }

        public void UpdateSegmentLengths()
        {
            children[0].UpdateLength();
            children[1].UpdateLength();
            this.Parent.UpdateLength();
        }

        public void UpdatePhysicalLocal()
        {
            downstream[0].UpdatePhysicalLocal();
            downstream[1].UpdatePhysicalLocal();
            this.Upstream.UpdatePhysicalLocal();
        }

        public void UpdatePhysicalGlobalAndPropagate()
        {
            downstream[0].UpdatePhysicalGlobal();
            downstream[1].UpdatePhysicalGlobal();
            PropagatePhysicalUpstream();
        }

        public void UpdatePhysicalAndPropagate()
        {
            UpdateSegmentLengths();
            UpdatePhysicalLocal();
            UpdatePhysicalGlobalAndPropagate();
        }

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

        public void UpdateLogicalAndPropagate()
        {
            downstream[0].UpdateLogical();
            downstream[1].UpdateLogical();
            PropagateLogicalUpstream();
        }

        public double MinOuterLength()
        {
            var p1 = Vector3.DistanceSquared(this.Upstream.Start.Position, downstream[0].End.Position);
            var p2 = Vector3.DistanceSquared(this.Upstream.Start.Position, downstream[1].End.Position);
            var cc = Vector3.DistanceSquared(downstream[0].End.Position, downstream[1].End.Position);
            return Math.Sqrt(Math.Min(cc, Math.Min(p1, p2)));
        }

        public double MinInnerLength()
        {
            var p = Vector3.DistanceSquared(this.Position, this.Upstream.Start.Position);
            var c0 = Vector3.DistanceSquared(this.Position, downstream[0].End.Position);
            var c1 = Vector3.DistanceSquared(this.Position, downstream[1].End.Position);
            return Math.Sqrt(Math.Min(p, Math.Min(c0, c1)));
        }

        public double MaxOuterLength()
        {
            var p1 = Vector3.DistanceSquared(this.Upstream.Start.Position, downstream[0].End.Position);
            var p2 = Vector3.DistanceSquared(this.Upstream.Start.Position, downstream[1].End.Position);
            var cc = Vector3.DistanceSquared(downstream[0].End.Position, downstream[1].End.Position);
            return Math.Sqrt(Math.Max(cc, Math.Max(p1, p2)));
        }

        public double MaxInnerLength()
        {
            var p = Vector3.DistanceSquared(this.Position, this.Upstream.Start.Position);
            var c0 = Vector3.DistanceSquared(this.Position, downstream[0].End.Position);
            var c1 = Vector3.DistanceSquared(this.Position, downstream[1].End.Position);
            return Math.Sqrt(Math.Max(p, Math.Max(c0, c1)));
        }

        public double BifurcationRatio => f0 > f1 ? f1 / f0 : f0 / f1;

        public (double f0, double f1) Fractions => (f0, f1);

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

        public int IndexOf(Branch branch)
        {
            return downstream[0] == branch ? 0 : downstream[1] == branch ? 1 : -1;
        }
    }
}
