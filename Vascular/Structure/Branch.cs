using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure.Nodes;

namespace Vascular.Structure
{
    [DataContract]
    public class Branch : IAxialBoundsQueryable<Segment>, IAxialBoundable
    {
        [DataMember]
        private List<Segment> segments = new List<Segment>();

        public Branch()
        {

        }

        public Branch(Segment s)
        {
            Initialize(s);
        }

        public void Initialize(Segment s)
        {
            segments.Clear();
            Add(s);
            while (s.End is Transient t)
            {
                s = t.Child;
                Add(s);
            }
        }

        public void Reinitialize()
        {
            Initialize(segments[0]);
        }

        public void Initialize()
        {
            segments.Clear();
            var s = this.End.Parent;
            Add(s);
            while (s.Start is Transient t)
            {
                s = t.Parent;
                Add(s);
            }
            segments.Reverse();
        }

        public void SetEndpoints()
        {
            if (!(segments[0].Start is BranchNode start))
            {
                throw new TopologyException("Branch does not start with BranchNode instance");
            }
            if (!(segments[^1].End is BranchNode end))
            {
                throw new TopologyException("Branch does not end with BranchNode instance");
            }
            this.Start = start;
            this.End = end;
        }

        private void Add(Segment s)
        {
            segments.Add(s);
            s.Branch = this;
        }

        public void Reset()
        {
            var s = segments[0];
            segments = new List<Segment>() { s };
            s.End = this.End;
            this.End.Parent = s;
        }

        public IReadOnlyList<Segment> Segments
        {
            get
            {
                return segments;
            }
        }

        public IEnumerable<INode> Nodes
        {
            get
            {
                yield return this.Start;
                foreach (var s in segments)
                {
                    yield return s.End;
                }
            }
        }

        public IEnumerable<INode> Transients
        {
            get
            {
                for (var i = 1; i < segments.Count; ++i)
                {
                    yield return segments[i].Start;
                }
            }
        }

        public IReadOnlyList<INode> GetTransients()
        {
            var list = new List<INode>(segments.Count - 1);
            for (var i = 0; i < segments.Count - 1; ++i)
            {
                list.Add(segments[i].End);
            }
            return list;
        }

        [DataMember]
        public BranchNode Start { get; set; } = null;

        public Branch Parent
        {
            get
            {
                return this.Start.Upstream;
            }
        }

        [DataMember]
        public BranchNode End { get; set; } = null;

        public Branch[] Children
        {
            get
            {
                return this.End.Downstream;
            }
        }

        public Network Network
        {
            get
            {
                return this.End.Network;
            }
        }

        public Vector3 Direction
        {
            get
            {
                return this.End.Position - this.Start.Position;
            }
        }

        public Vector3 NormalizedDirection
        {
            get
            {
                return this.Direction.Normalize();
            }
        }

        public double DirectLength
        {
            get
            {
                return this.Direction.Length;
            }
        }

        public double Tortuosity
        {
            get
            {
                return this.Length / this.DirectLength;
            }
        }

        public double Slenderness
        {
            get
            {
                return this.Length / this.Radius;
            }
        }

        public bool IsTerminal
        {
            get
            {
                return this.End is Terminal;
            }
        }

        [DataMember]
        private double radius = 0.0;

        public double Radius
        {
            get
            {
                return radius;
            }
            set
            {
                if (value >= 0.0)
                {
                    radius = value;
                }
            }
        }

        [DataMember]
        public double Length { get; private set; } = 0.0;

        [DataMember]
        public double EffectiveLength { get; private set; } = 0.0;

        [DataMember]
        private double reducedResistanceLocal = 0.0;

        [DataMember]
        public double ReducedResistance { get; private set; } = 0.0;

        public double Resistance
        {
            get
            {
                return reducedResistanceLocal / Math.Pow(radius, 4);
            }
        }

        public void UpdateLengths()
        {
            foreach (var s in segments)
            {
                s.UpdateLength();
            }
        }

        public void UpdateRadii()
        {
            foreach (var s in segments)
            {
                s.UpdateRadius();
            }
        }

        public void UpdatePhysicalLocal()
        {
            this.Length = segments[0].Length;
            for (var i = 1; i < segments.Count; ++i)
            {
                this.Length += segments[i].Length;
            }
            reducedResistanceLocal = 8.0 * this.Network.Viscosity * this.Length / Math.PI;
        }

        public void UpdatePhysicalGlobal()
        {
            this.EffectiveLength = this.Length + this.End.EffectiveLength;
            this.ReducedResistance = reducedResistanceLocal + this.End.ReducedResistance;
        }

        public void PropagatePhysicalUpstream()
        {
            UpdatePhysicalGlobal();
            this.Start.PropagatePhysicalUpstream();
        }

        [DataMember]
        public double Flow { get; private set; } = 0.0;

        public void SetFlow(double Q)
        {
            this.Flow = Q;
        }

        public void UpdateLogical()
        {
            this.Flow = this.End.Flow;
        }

        public void SetLogical()
        {
            foreach (var c in this.Children)
            {
                c.SetLogical();
            }
            UpdateLogical();
        }

        public void PropagateLogicalUpstream()
        {
            UpdateLogical();
            this.Start.PropagateLogicalUpstream();
        }

        [DataMember]
        public AxialBounds LocalBounds { get; private set; } = new AxialBounds();

        public AxialBounds GenerateLocalBounds()
        {
            this.LocalBounds = new AxialBounds(segments[0].GenerateBounds());
            for (var i = 1; i < segments.Count; ++i)
            {
                this.LocalBounds.Append(segments[i].GenerateBounds());
            }
            return this.LocalBounds;
        }

        public AxialBounds GenerateLocalBounds(double pad)
        {
            this.LocalBounds = new AxialBounds(segments[0].GenerateBounds(pad));
            for (var i = 1; i < segments.Count; ++i)
            {
                this.LocalBounds.Append(segments[i].GenerateBounds(pad));
            }
            return this.LocalBounds;
        }

        [DataMember]
        public AxialBounds GlobalBounds { get; private set; } = new AxialBounds();

        public AxialBounds GenerateDownstreamBounds()
        {
            GenerateLocalBounds();
            var downstream = this.End.GenerateDownstreamBounds();
            this.GlobalBounds = this.LocalBounds + downstream;
            return this.GlobalBounds;
        }

        public AxialBounds GenerateDownstreamBounds(double pad)
        {
            GenerateLocalBounds(pad);
            var downstream = this.End.GenerateDownstreamBounds(pad);
            this.GlobalBounds = this.LocalBounds + downstream;
            return this.GlobalBounds;
        }

        public AxialBounds GetAxialBounds()
        {
            return this.LocalBounds;
        }

        public void Query(AxialBounds query, Action<Segment> action)
        {
            foreach (var s in segments)
            {
                if (s.Bounds.Intersects(query))
                {
                    action(s);
                }
            }
        }

        public int Depth
        {
            get
            {
                return this.End.Depth;
            }
        }

        public Branch GetNthUpstream(int n)
        {
            var u = this;
            while (--n >= 0 && u != null)
            {
                u = u.Start.Upstream;
            }
            return u;
        }

        public bool IsTopologicallyValid
        {
            get
            {
                return ReferenceEquals(this, this.CurrentTopologicallyValid);
            }
        }

        public Branch CurrentTopologicallyValid
        {
            get
            {
                return this.End?.Upstream;
            }
        }

        public bool IsAncestorOf(Branch b)
        {
            while (b != null)
            {
                if (ReferenceEquals(b, this))
                {
                    return true;
                }
                b = b.Parent;
            }
            return false;
        }

        public bool IsStrictAncestorOf(Branch b)
        {
            b = b.Parent;
            while (b != null)
            {
                if (ReferenceEquals(b, this))
                {
                    return true;
                }
                b = b.Parent;
            }
            return false;
        }

        public bool IsSiblingOf(Branch b)
        {
            return ReferenceEquals(this.Start, b.Start);
        }

        public bool IsChildOf(Branch b)
        {
            return ReferenceEquals(this.Start, b.End);
        }

        public bool IsParentOf(Branch b)
        {
            return ReferenceEquals(this.End, b.Start);
        }

        public bool IsRooted
        {
            get
            {
                var b = this;
                while (b.Parent != null)
                {
                    b = b.Parent;
                }
                return b.Start is Source;
            }
        }

        public static Branch CommonAncestor(Branch a, Branch b)
        {
            while (!ReferenceEquals(a, b))
            {
                if (a.Flow > b.Flow)
                {
                    b = b.Parent;
                }
                else
                {
                    a = a.Parent;
                }
            }
            return a;
        }

        public IEnumerator<Segment> GetEnumerator()
        {
            return segments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
