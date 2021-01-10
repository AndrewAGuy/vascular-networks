using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
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
            get
            {
                return child;
            }
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

        sealed public override Segment[] Children
        {
            get
            {
                return children;
            }
        }

        public void SetPosition(Vector3 x)
        {
            position = x ?? throw new PhysicalValueException("Source position must not be null");
        }

        sealed public override int Depth
        {
            get
            {
                return 0;
            }
        }

        sealed public override double PathLength
        {
            get
            {
                return 0.0;
            }
        }

        sealed public override double Flow
        {
            get
            {
                return child.Flow;
            }
        }

        sealed public override Branch[] Downstream
        {
            get
            {
                return downstream;
            }
        }

        sealed public override Segment Parent
        {
            get
            {
                return null;
            }
            set
            {
                throw new TopologyException("Source node has no parent");
            }
        }

        sealed public override Branch Upstream
        {
            get
            {
                return null;
            }
        }

        sealed public override Vector3 Position
        {
            get
            {
                return position;
            }
            set
            {
                throw new GeometryException("Source node position is fixed");
            }
        }

        sealed public override double EffectiveLength
        {
            get
            {
                return down.EffectiveLength;
            }
        }

        sealed public override double ReducedResistance
        {
            get
            {
                return down.EffectiveLength;
            }
        }

        sealed public override void PropagateLogicalUpstream()
        {
            return;
        }

        sealed public override void PropagatePhysicalUpstream()
        {
            return;
        }

        public abstract double RootRadius { get; }

        public abstract double Volume { get; }

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

        sealed public override void CalculatePathLengthsAndDepths()
        {
            down.End.CalculatePathLengthsAndDepths();
        }

        sealed public override void CalculatePhysical()
        {
            down.UpdateLengths();
            down.UpdatePhysicalLocal();
            down.End.CalculatePhysical();
            down.UpdatePhysicalGlobal();
        }

        sealed public override void CalculatePressures()
        {
            down.End.CalculatePressures();
        }

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
