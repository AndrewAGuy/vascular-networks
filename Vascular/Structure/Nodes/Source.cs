using System;
using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Structure.Nodes
{
    [DataContract]
    [KnownType(typeof(PressureSource))]
    [KnownType(typeof(RadiusSource))]
    public abstract class Source : BranchNode
    {
        public Source(Vector3 x)
        {
            SetPosition(x);
        }

        [DataMember]
        private Vector3 position = null;

        [DataMember]
        private Segment child = null;
        [DataMember]
        private Branch down = null;
        [DataMember]
        private readonly Segment[] children = new Segment[1] { null };
        [DataMember]
        private readonly Branch[] downstream = new Branch[1] { null };

        public Segment Child
        {
            get => child;
            set
            {
                child = value;
                children[0] = value;
                if (value != null)
                {
                    down = value.Branch;
                    downstream[0] = value.Branch;
                }
                else
                {
                    down = null;
                    downstream[0] = null;
                }
            }
        }

        sealed public override Segment[] Children => children;

        public void SetPosition(Vector3 x)
        {
            position = x ?? throw new PhysicalValueException("Source position must not be null");
        }

        sealed public override double Flow => child.Flow;

        sealed public override Branch[] Downstream => downstream;

        sealed public override Segment Parent
        {
            get => null;
            set => throw new TopologyException("Source node has no parent");
        }

        sealed public override Branch Upstream => null;

        sealed public override Vector3 Position
        {
            get => position;
            set => throw new GeometryException("Source node position is fixed");
        }

#if !NoEffectiveLength
        sealed public override double EffectiveLength => down.EffectiveLength;

        public abstract double Volume { get; }
#endif

        public sealed override double ReducedResistance => down.ReducedResistance;

        public abstract double Resistance { get; }

        public abstract double Work { get; }

        sealed public override void PropagateLogicalUpstream()
        {
            return;
        }

        sealed public override void PropagatePhysicalUpstream()
        {
            return;
        }

        public abstract double RootRadius { get; }

        sealed public override void SetChildRadii()
        {
            down.Radius = this.RootRadius;
        }

        sealed public override void PropagateRadiiDownstream()
        {
            SetChildRadii();
            down.UpdateRadii();
            down.End.PropagateRadiiDownstream();
        }

        sealed public override void PropagateRadiiDownstream(double pad)
        {
            SetChildRadii();
            down.End.PropagateRadiiDownstream(pad);
            down.Radius += pad;
            down.UpdateRadii();
        }

        public sealed override void PropagateRadiiDownstream(Func<Branch, double> postProcessing)
        {
            SetChildRadii();
            down.End.PropagateRadiiDownstream(postProcessing);
            down.Radius = postProcessing(down);
            down.UpdateRadii();
        }

#if !NoDepthPathLength
        sealed public override void CalculatePathLengthsAndDepths()
        {
            down.End.CalculatePathLengthsAndDepths();
        }

        sealed public override double PathLength => 0.0;

        sealed public override int Depth => 0;
#endif

        sealed public override void CalculatePhysical()
        {
            down.UpdateLengths();
            down.UpdatePhysicalLocal();
            down.End.CalculatePhysical();
            down.UpdatePhysicalGlobal();
        }

#if !NoPressure
        sealed public override void CalculatePressures()
        {
            down.End.CalculatePressures();
        }
#endif

        sealed public override AxialBounds GenerateDownstreamBounds()
        {
            return down.GenerateDownstreamBounds();
        }

        sealed public override AxialBounds GenerateDownstreamBounds(double pad)
        {
            return down.GenerateDownstreamBounds(pad);
        }

        public abstract void SetTargetRadius(double target, double current);
    }
}
