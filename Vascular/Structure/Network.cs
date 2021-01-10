using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure.Nodes;
using Vascular.Structure.Splitting;

namespace Vascular.Structure
{
    [DataContract]
    public class Network : IAxialBoundsQueryable<Segment>, IAxialBoundsQueryable<Branch>
    {
        [DataMember]
        private Source source;

        public Source Source
        {
            get
            {
                return source;
            }
            set
            {
                source = value;
                if (source != null)
                {
                    source.Network = this;
                }
            }
        }

        public Branch Root
        {
            get
            {
                return source.Child?.Branch;
            }
        }

        [DataMember]
        public Network[] Partners { get; set; } = null;

        [DataMember]
        public double Viscosity { get; set; } = 4.0e-6;

        [DataMember]
        public double PressureOffset { get; set; } = 0.0;

        [DataMember]
        public ISplittingFunction Splitting { get; set; } = new Murray() { Exponent = 3.0 };

        [DataMember]
        public double RelativeCompliance { get; set; } = 1;

        [DataMember]
        public bool Output { get; set; } = false;

        [DataMember]
        public string Name { get; set; } = "";

        [DataMember]
        public Vector3 InletDirection { get; set; } = null;

        //public Func<Terminal, Vector3> TerminalDirection { get; set; } = t => null;

        public Network Clone(Source s)
        {
            var n = new Network()
            {
                Source = s,
                Splitting = this.Splitting
            };
            var r = CloneDownstream(n, this.Root);
            s.Child = r.Segments[0];
            r.Start = s;
            r.Segments[0].Start = s;
            return n;
        }

        private static Branch CloneDownstream(Network n, Branch b)
        {
            var T = new List<Transient>(b.Segments.Count);
            foreach (var t in b.Transients)
            {
                T.Add(new Transient() { Position = t.Position });
            }
            var f = new Segment();
            var l = f;
            foreach (var t in T)
            {
                t.Parent = l;
                l.End = t;
                l = new Segment();
                t.Child = l;
                l.Start = t;
            }
            var c = new Branch();
            switch (b.End)
            {
                case Terminal t:
                    c.End = new Terminal(t.Position, t.Flow)
                    {
                        Parent = l,
                        Network = n
                    };
                    l.End = c.End;
                    break;
                case Bifurcation s:
                    c.End = new Bifurcation()
                    {
                        Parent = l,
                        Position = s.Position,
                        Network = n
                    };
                    l.End = c.End;
                    var c0 = CloneDownstream(n, s.Downstream[0]);
                    c0.Segments[0].Start = c.End;
                    c0.Start = c.End;
                    c.End.Children[0] = c0.Segments[0];
                    c.End.Downstream[0] = c0;
                    var c1 = CloneDownstream(n, s.Downstream[1]);
                    c1.Segments[0].Start = c.End;
                    c1.Start = c.End;
                    c.End.Children[1] = c1.Segments[0];
                    c.End.Downstream[1] = c1;
                    break;
                default:
                    throw new TopologyException("Branch ended with invalid node");
            }
            c.Initialise(f);
            return c;
        }

        public AxialBounds GetAxialBounds()
        {
            return this.Root.GlobalBounds;
        }

        public void Query(AxialBounds query, Action<Branch> action)
        {
            BranchQuery(query, action, this.Root);
        }

        public static void BranchQuery(AxialBounds query, Action<Branch> action, Branch branch)
        {
            if (query.Intersects(branch.LocalBounds))
            {
                action(branch);
            }
            foreach (var child in branch.Children)
            {
                if (query.Intersects(child.GlobalBounds))
                {
                    BranchQuery(query, action, child);
                }
            }
        }

        public void Query(AxialBounds query, Action<Segment> action)
        {
            SegmentQuery(query, action, this.Root);
        }

        public static void SegmentQuery(AxialBounds query, Action<Segment> action, Branch branch)
        {
            if (query.Intersects(branch.LocalBounds))
            {
                foreach (var s in branch.Segments)
                {
                    if (query.Intersects(s.Bounds))
                    {
                        action(s);
                    }
                }
            }
            foreach (var child in branch.Children)
            {
                if (query.Intersects(child.GlobalBounds))
                {
                    SegmentQuery(query, action, child);
                }
            }
        }

        public IEnumerator<Segment> GetEnumerator()
        {
            return this.Segments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield break;
        }

        IEnumerator<Branch> IEnumerable<Branch>.GetEnumerator()
        {
            return this.Branches.GetEnumerator();
        }

        public IEnumerable<Branch> Branches
        {
            get
            {
                var stack = new Stack<Branch>();
                stack.Push(this.Root);
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
        }

        public IEnumerable<Segment> Segments
        {
            get
            {
                foreach (var branch in this.Branches)
                {
                    foreach (var segment in branch.Segments)
                    {
                        yield return segment;
                    }
                }
            }
        }

        public IEnumerable<BranchNode> BranchNodes
        {
            get
            {
                yield return this.Source;
                foreach (var branch in this.Branches)
                {
                    yield return branch.End;
                }
            }
        }

        public IEnumerable<INode> Nodes
        {
            get
            {
                yield return this.Source;
                foreach (var segment in this.Segments)
                {
                    yield return segment.End;
                }
            }
        }

        public IEnumerable<Terminal> Terminals
        {
            get
            {
                var stack = new Stack<Branch>();
                stack.Push(this.Root);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (current.End is Terminal terminal)
                    {
                        yield return terminal;
                    }
                    else
                    {
                        var children = current.Children;
                        for (var i = 0; i < children.Length; ++i)
                        {
                            stack.Push(children[i]);
                        }
                    }
                }
            }
        }
    }
}
