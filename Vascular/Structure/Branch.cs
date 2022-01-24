using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure.Nodes;

namespace Vascular.Structure
{
    /// <summary>
    /// Links <see cref="BranchNode"/> together, allowing more efficient traversal in highly fragmented branches compared to
    /// using only <see cref="Segment"/>.
    /// </summary>
    [DataContract]
    public class Branch : IAxialBoundsQueryable<Segment>, IAxialBoundable
    {
        [DataMember]
        private List<Segment> segments = new(1);

        /// <summary>
        /// 
        /// </summary>
        public Branch()
        {

        }

        /// <summary>
        /// See <see cref="Initialize(Segment)"/>.
        /// </summary>
        /// <param name="s"></param>
        public Branch(Segment s)
        {
            Initialize(s);
        }

        /// <summary>
        /// Works down from <paramref name="s"/> until a <see cref="BranchNode"/> is hit.
        /// </summary>
        /// <param name="s"></param>
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

        /// <summary>
        /// From the first segment, walk to the next <see cref="BranchNode"/> and update <see cref="Segments"/>.
        /// </summary>
        public void Reinitialize()
        {
            Initialize(segments[0]);
        }

        /// <summary>
        /// Works up from <see cref="End"/> until a <see cref="BranchNode"/> is hit.
        /// </summary>
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

        /// <summary>
        /// If the segment list is complete, initialize from this.
        /// </summary>
        public void SetEndpoints()
        {
            if (segments[0].Start is not BranchNode start)
            {
                throw new TopologyException("Branch does not start with BranchNode instance");
            }
            if (segments[^1].End is not BranchNode end)
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

        /// <summary>
        /// Link <see cref="Start"/> and <see cref="End"/> by a single <see cref="Segment"/>.
        /// </summary>
        public void Reset()
        {
            var s = segments[0];
            segments = new List<Segment>(1) { s };
            s.End = this.End;
            this.End.Parent = s;
        }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<Segment> Segments => segments;

        /// <summary>
        /// Includes <see cref="Start"/>, <see cref="End"/>, <see cref="Transients"/>.
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<INode> GetTransients()
        {
            var list = new List<INode>(segments.Count - 1);
            for (var i = 0; i < segments.Count - 1; ++i)
            {
                list.Add(segments[i].End);
            }
            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public BranchNode Start { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public Branch Parent => this.Start.Upstream;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public BranchNode End { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public Branch[] Children => this.End.Downstream;

        /// <summary>
        /// 
        /// </summary>
        public Network Network => this.End.Network;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Direction => this.End.Position - this.Start.Position;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 NormalizedDirection => this.Direction.Normalize();

        /// <summary>
        /// 
        /// </summary>
        public double DirectLength => this.Direction.Length;

        /// <summary>
        /// Most simple estimate, arc length / chord length.
        /// </summary>
        public double Tortuosity => this.Length / this.DirectLength;

        /// <summary>
        /// Radial slenderness.
        /// </summary>
        public double Slenderness => this.Length / this.Radius;

        /// <summary>
        /// 
        /// </summary>
        public bool IsTerminal => this.End is Terminal;

        [DataMember]
        private double radius = 0.0;

        /// <summary>
        /// The branch radius. Ensure that the sum over <see cref="Segments"/> of <c>s.L / pow(s.r, 4)</c> is compatible with this radius.
        /// </summary>
        public double Radius
        {
            get => radius;
            set
            {
                if (value >= 0.0)
                {
                    radius = value;
                }
            }
        }

        /// <summary>
        /// The cached sum of <see cref="Segment.Length"/>.
        /// </summary>
        [DataMember]
        public double Length { get; private set; } = 0.0;

        /// <summary>
        /// Defined as <c>R * pow(r, 4)</c>, is propagated upstream. Allows radii ratios to be set without knowing the actual values.
        /// In this case, we always define <c>8 * mu / pi = 1</c> for efficiency, so is not the 'true' reduced resistance. 
        /// </summary>
        [DataMember]
        public double ReducedResistance { get; private set; } = 0.0;

        /// <summary>
        /// Assumes Hagen-Poiseuille flow.
        /// </summary>
        public double Resistance => this.Length * this.Network.ScaledViscosity / Math.Pow(radius, 4);

        /// <summary>
        /// Rrecalculates the lengths of each <see cref="Segment"/> in the branch. 
        /// Does not update <see cref="Length"/> - for this, use <see cref="UpdatePhysicalLocal"/>.
        /// </summary>
        public void UpdateLengths()
        {
            foreach (var s in segments)
            {
                s.UpdateLength();
            }
        }

        /// <summary>
        /// Sets each radius to <see cref="Radius"/>.
        /// </summary>
        public void UpdateRadii()
        {
            foreach (var s in segments)
            {
                s.UpdateRadius();
            }
        }

        /// <summary>
        /// Updates <see cref="Length"/>.
        /// </summary>
        public void UpdatePhysicalLocal()
        {
            this.Length = segments[0].Length;
            for (var i = 1; i < segments.Count; ++i)
            {
                this.Length += segments[i].Length;
            }
        }

#if !NoEffectiveLength
        /// <summary>
        /// For efficient update of volume. Introduced in DOI: 10.1109/TBME.2019.2942313
        /// </summary>
        [DataMember]
        public double EffectiveLength { get; private set; } = 0.0;
#endif

        /// <summary>
        /// Updates the physical (legacy naming for geometric) parameters that are propagated upstream.
        /// </summary>
        public void UpdatePhysicalGlobal()
        {
#if !NoEffectiveLength
            this.EffectiveLength = this.Length + this.End.EffectiveLength;
#endif
            this.ReducedResistance = this.Length + this.End.ReducedResistance;
        }

        /// <summary>
        /// Updates then propagates upstream. See <see cref="UpdatePhysicalGlobal"/>, <see cref="BranchNode.PropagatePhysicalUpstream"/>.
        /// </summary>
        public void PropagatePhysicalUpstream()
        {
            UpdatePhysicalGlobal();
            this.Start.PropagatePhysicalUpstream();
        }

        /// <summary>
        /// By default, this is set through <see cref="BranchNode.Flow"/> of <see cref="End"/>.
        /// Can be set for testing hypothetical scenarios using <see cref="SetFlow(double)"/>.
        /// </summary>
        [DataMember]
        public double Flow { get; private set; } = 0.0;

        /// <summary>
        /// See <see cref="Flow"/>.
        /// </summary>
        /// <param name="Q"></param>
        public void SetFlow(double Q)
        {
            this.Flow = Q;
        }

        /// <summary>
        /// Updates logical (legacy naming for topological) fields that are propagated upstream.
        /// </summary>
        public void UpdateLogical()
        {
            this.Flow = this.End.Flow;
        }

        /// <summary>
        /// Use in a single down-up pass to recalculate all flows.
        /// </summary>
        public void SetLogical()
        {
            foreach (var c in this.Children)
            {
                c.SetLogical();
            }
            UpdateLogical();
        }

        /// <summary>
        /// Propagates the flow changes up to the root.
        /// </summary>
        public void PropagateLogicalUpstream()
        {
            UpdateLogical();
            this.Start.PropagateLogicalUpstream();
        }

        /// <summary>
        /// The <see cref="AxialBounds"/> containing all <see cref="Segments"/>.
        /// </summary>
        [DataMember]
        public AxialBounds LocalBounds { get; private set; } = new AxialBounds();

        /// <summary>
        /// Updates each <see cref="Segment.Bounds"/> in <see cref="Segments"/>, then <see cref="LocalBounds"/> from these.
        /// </summary>
        /// <returns></returns>
        public AxialBounds GenerateLocalBounds()
        {
            this.LocalBounds = new AxialBounds(segments[0].GenerateBounds());
            for (var i = 1; i < segments.Count; ++i)
            {
                this.LocalBounds.Append(segments[i].GenerateBounds());
            }
            return this.LocalBounds;
        }

        /// <summary>
        /// Updates each <see cref="Segment.Bounds"/> in <see cref="Segments"/>, extending using <paramref name="pad"/>, 
        /// then <see cref="LocalBounds"/> from these.
        /// </summary>
        /// <param name="pad"></param>
        /// <returns></returns>
        public AxialBounds GenerateLocalBounds(double pad)
        {
            this.LocalBounds = new AxialBounds(segments[0].GenerateBounds(pad));
            for (var i = 1; i < segments.Count; ++i)
            {
                this.LocalBounds.Append(segments[i].GenerateBounds(pad));
            }
            return this.LocalBounds;
        }

        /// <summary>
        /// The <see cref="AxialBounds"/> bounding <see cref="LocalBounds"/> and the result generated by
        /// <see cref="BranchNode.GenerateDownstreamBounds()"/>.
        /// </summary>
        [DataMember]
        public AxialBounds GlobalBounds { get; private set; } = new AxialBounds();

        /// <summary>
        /// Generates <see cref="LocalBounds"/>, then combines with <see cref="BranchNode.GenerateDownstreamBounds()"/> to update
        /// <see cref="GlobalBounds"/>.
        /// </summary>
        /// <returns></returns>
        public AxialBounds GenerateDownstreamBounds()
        {
            GenerateLocalBounds();
            var downstream = this.End.GenerateDownstreamBounds();
            this.GlobalBounds = this.LocalBounds + downstream;
            return this.GlobalBounds;
        }

        /// <summary>
        /// See <see cref="GenerateDownstreamBounds()"/>, but calls <see cref="GenerateDownstreamBounds(double)"/> and
        /// <see cref="BranchNode.GenerateDownstreamBounds(double)"/>.
        /// </summary>
        /// <param name="pad"></param>
        /// <returns></returns>
        public AxialBounds GenerateDownstreamBounds(double pad)
        {
            GenerateLocalBounds(pad);
            var downstream = this.End.GenerateDownstreamBounds(pad);
            this.GlobalBounds = this.LocalBounds + downstream;
            return this.GlobalBounds;
        }

        /// <summary>
        /// Returns <see cref="LocalBounds"/>.
        /// </summary>
        /// <returns></returns>
        public AxialBounds GetAxialBounds()
        {
            return this.LocalBounds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
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

#if !NoDepthPathLength
        /// <summary>
        /// Returns the depth of the end node, so the <see cref="Network.Root"/> has depth 1.
        /// </summary>
        public int Depth => this.End.Depth;
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public Branch GetNthUpstream(int n)
        {
            var u = this;
            while (--n >= 0 && u != null)
            {
                u = u.Start.Upstream;
            }
            return u;
        }

        /// <summary>
        /// If we went to <see cref="End"/> and then <see cref="BranchNode.Upstream"/>, would we be back here?
        /// See <see cref="CurrentTopologicallyValid"/>.
        /// </summary>
        public bool IsTopologicallyValid => ReferenceEquals(this, this.CurrentTopologicallyValid);

        /// <summary>
        /// Sometimes branches are rewired but references are held elsewhere. This returns the branch that is
        /// the <see cref="BranchNode.Upstream"/> of the node <see cref="End"/>.
        /// </summary>
        public Branch CurrentTopologicallyValid => this.End?.Upstream;

        /// <summary>
        /// Similar to <see cref="IsStrictAncestorOf(Branch)"/> but allows b &lt;= a
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Tests if b &lt; a
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool IsSiblingOf(Branch b)
        {
            return ReferenceEquals(this.Start, b.Start);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool IsChildOf(Branch b)
        {
            return ReferenceEquals(this.Start, b.End);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool IsParentOf(Branch b)
        {
            return ReferenceEquals(this.End, b.Start);
        }

        /// <summary>
        /// Test if we can get to a source node from here.
        /// </summary>
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

        /// <summary>
        /// These must belong to the same <see cref="Network"/>, else null dereferencing will occur.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Similar to <see cref="CommonAncestor(Branch, Branch)"/>, but returns null if the branch with lower flow
        /// is a root vessel while iterating.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Branch CommonAncestorSafe(Branch a, Branch b)
        {
            while (!ReferenceEquals(a, b))
            {
                if (a.Flow > b.Flow)
                {
                    b = b.Parent;
                    if (b == null)
                    {
                        return null;
                    }
                }
                else
                {
                    a = a.Parent;
                    if (a == null)
                    {
                        return null;
                    }
                }
            }
            return a;
        }

        /// <summary>
        /// Calls <see cref="CommonAncestor(Branch, Branch)"/> repeatedly on elements of <paramref name="B"/>.
        /// If empty, returns null.
        /// </summary>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Branch CommonAncestor(IEnumerable<Branch> B)
        {
            var e = B.GetEnumerator();
            if (!e.MoveNext())
            {
                return null;
            }
            var a = e.Current;
            while (e.MoveNext())
            {
                a = CommonAncestor(a, e.Current);
            }
            return a;
        }

        /// <summary>
        /// Similar to <see cref="CommonAncestor(IEnumerable{Branch})"/>, but returns null if the branches reside in
        /// different trees.
        /// </summary>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Branch CommonAncestorSafe(IEnumerable<Branch> B)
        {
            var e = B.GetEnumerator();
            if (!e.MoveNext())
            {
                return null;
            }
            var a = e.Current;
            while (e.MoveNext())
            {
                a = CommonAncestorSafe(a, e.Current);
                if (a == null)
                {
                    return null;
                }
            }
            return a;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Segment> GetEnumerator()
        {
            return segments.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerates all branches between this and <paramref name="target"/>, exclusive of the endpoints.
        /// If <paramref name="target"/> is not upstream of this, will enumerate all upstream to root, inclusive of the root.
        /// Note: If <paramref name="target"/> equals the calling instance, the termination condition will not be met -
        /// if this should behave as an empty sequence then it is important to test for equality beforehand.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public IEnumerable<Branch> UpstreamTo(Branch target)
        {
            var b = this.Parent;
            while (b != null && b != target)
            {
                yield return b;
                b = b.Parent;
            }
        }

        /// <summary>
        /// Enumerates all downstream branches, excluding the current branch.
        /// To do this without allocating a new stack each time, see <see cref="Diagnostics.BranchEnumerator"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Branch> DownstreamOf()
        {
            var stack = new Stack<Branch>();
            foreach (var c in this.Children)
            {
                stack.Push(c);
            }
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;
                var children = current.Children;
                for (var i = 0; i < children.Length; ++i)
                {
                    stack.Push(children[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Branch FirstSibling
        {
            get
            {
                foreach (var s in this.Start.Downstream)
                {
                    if (s != this)
                    {
                        return s;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int IndexInParent => Array.IndexOf(this.Start.Downstream, this);
    }
}
