using System;
using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure.Nodes;

namespace Vascular.Structure
{
    [DataContract]
    [KnownType(typeof(RadiusSource))]
    [KnownType(typeof(PressureSource))]
    [KnownType(typeof(Bifurcation))]
    [KnownType(typeof(Terminal))]
    public abstract class BranchNode : INode
    {
#if !NoPressure
        public abstract double Pressure { get; }

        public abstract void CalculatePressures();
#endif

#if !NoDepthPathLength
        public abstract double PathLength { get; }

        public abstract int Depth { get; }

        public abstract void CalculatePathLengthsAndDepths();
#endif

        public abstract Segment Parent { get; set; }

        public abstract Segment[] Children { get; }

        public abstract Vector3 Position { get; set; }

        public abstract Branch Upstream { get; }

        public abstract Branch[] Downstream { get; }

        [DataMember]
        public Network Network { get; set; } = null;

        public abstract double Flow { get; }

#if !NoEffectiveLength
        public abstract double EffectiveLength { get; }
#endif

        public abstract double ReducedResistance { get; }

        public abstract void PropagateLogicalUpstream();

        public abstract void PropagatePhysicalUpstream();

        public abstract void SetChildRadii();

        public abstract void PropagateRadiiDownstream();

        public abstract void PropagateRadiiDownstream(double pad);

        public abstract void PropagateRadiiDownstream(Func<Branch, double> postProcessing);

        public abstract void CalculatePhysical();

        public abstract AxialBounds GenerateDownstreamBounds();

        public abstract AxialBounds GenerateDownstreamBounds(double pad);

        public void ForEach(Action<Branch> action)
        {
            foreach (var c in this.Downstream)
            {
                action(c);
                c.End.ForEach(action);
            }
        }
    }
}
