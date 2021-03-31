using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Structure.Nodes
{
    [DataContract]
    public class Terminal : BranchNode
    {
        public Terminal(Vector3 x, double Q)
        {
            SetPosition(x);
            SetFlow(Q);
        }

        [DataMember]
        private Vector3 position = null;
        [DataMember]
        private double flow;

        private static readonly Segment[] CHILDREN = Array.Empty<Segment>();
        private static readonly Branch[] DOWNSTREAM = Array.Empty<Branch>();

        [DataMember]
        public override Segment Parent { get; set; } = null;

        public override Segment[] Children => CHILDREN;

        public void SetPosition(Vector3 x)
        {
            position = x ?? throw new PhysicalValueException("Terminal position must not be null");
        }

        public void SetFlow(double Q)
        {
            flow = Q > 0.0 ? Q : throw new PhysicalValueException($"Terminal flow rate must be positive: Q = {Q}");
        }

        public override Vector3 Position
        {
            get => position;
            set => throw new GeometryException("Terminal node position is fixed");
        }

#if !NoPressure
        public override double Pressure => 0.0;

        public override void CalculatePressures()
        {
            return;
        }
#endif

#if !NoEffectiveLength
        public override double EffectiveLength => 0.0;
#endif

        public override double ReducedResistance => 0.0;

#if !NoDepthPathLength
        [DataMember]
        private double pathLength = -1.0;
        [DataMember]
        private int depth = -1;

        public override double PathLength => pathLength;

        public override int Depth => depth;

        public override void CalculatePathLengthsAndDepths()
        {
            depth = this.Upstream.Start.Depth + 1;
            pathLength = this.Upstream.Start.PathLength + this.Upstream.Length;
        }
#endif

        [DataMember]
        public Terminal[] Partners { get; set; } = null;

        [DataMember]
        public bool Culled { get; set; } = false;

        public bool IsRooted => this.Upstream?.IsRooted ?? false;

        public override double Flow => flow;

        public override void CalculatePhysical()
        {
            return;
        }

        public override void PropagateRadiiDownstream()
        {
            return;
        }

        public override void PropagateRadiiDownstream(double pad)
        {
            return;
        }

        public override void PropagateRadiiDownstream(Func<Branch, double> postProcessing)
        {
            return;
        }

        public override AxialBounds GenerateDownstreamBounds()
        {
            return new AxialBounds();
        }

        public override AxialBounds GenerateDownstreamBounds(double pad)
        {
            return new AxialBounds();
        }

        public override void SetChildRadii()
        {
            return;
        }

        public override void PropagateLogicalUpstream()
        {
            this.Upstream.PropagateLogicalUpstream();
        }

        public override void PropagatePhysicalUpstream()
        {
            this.Upstream.PropagatePhysicalUpstream();
        }

        public override Branch Upstream => this.Parent?.Branch;

        public override Branch[] Downstream => DOWNSTREAM;

        public static void ForDownstream(Branch branch, Action<Terminal> action)
        {
            if (branch.End is Terminal terminal)
            {
                action(terminal);
            }
            else
            {
                foreach (var child in branch.Children)
                {
                    ForDownstream(child, action);
                }
            }
        }

        public static List<Terminal> GetDownstream(Branch branch, int capacity = 0)
        {
            var list = capacity > 0 ? new List<Terminal>(capacity) : new List<Terminal>();
            ForDownstream(branch, t => list.Add(t));
            return list;
        }

        public static int CountDownstream(Branch branch)
        {
            var count = 0;
            ForDownstream(branch, t => ++count);
            return count;
        }
    }
}
