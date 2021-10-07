using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure.Nodes;
using Vascular.Structure.Splitting;

namespace Vascular.Structure
{
    /// <summary>
    /// Wraps a <see cref="Nodes.Source"/> and some other associated data such as the <see cref="Viscosity"/> and <see cref="ISplittingFunction"/>.
    /// </summary>
    [DataContract]
    public class Network : IAxialBoundsQueryable<Segment>, IAxialBoundsQueryable<Branch>
    {
        [DataMember]
        private Source source;

        /// <summary>
        /// 
        /// </summary>
        public Source Source
        {
            get => source;
            set
            {
                source = value;
                if (source != null)
                {
                    source.Network = this;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Branch Root => source.Child?.Branch;

        /// <summary>
        /// The matching group this belongs to.
        /// </summary>
        [DataMember]
        public Network[] Partners { get; set; } = null;

        [DataMember]
        private double viscosity = 4.0e-6;
        [DataMember]
        private double scaledViscosity = 4.0e-6 * 8.0 / Math.PI;
        
        /// <summary>
        /// Typically set in kPa &#183; s.
        /// </summary>
        public double Viscosity
        {
            get => viscosity;
            set
            {
                if (value > 0)
                {
                    viscosity = value;
                    scaledViscosity = value * 8.0 / Math.PI;
                }
            }
        }

        /// <summary>
        /// Used in calculating resistances for Hagen-Poiseuille flow.
        /// </summary>
        public double ScaledViscosity => scaledViscosity;

        /// <summary>
        /// Typically in kPa.
        /// </summary>
        [DataMember]
        public double PressureOffset { get; set; } = 0.0;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public ISplittingFunction Splitting { get; set; } = new Murray() { Exponent = 3.0 };

        /// <summary>
        /// Used in collision resolution.
        /// </summary>
        [DataMember]
        public double RelativeCompliance { get; set; } = 1;

        /// <summary>
        /// Whether the flow is from <see cref="Terminal"/> to <see cref="Source"/>.
        /// </summary>
        [DataMember]
        public bool Output { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Name { get; set; } = "";

        /// <summary>
        /// Used in smoothing.
        /// </summary>
        [DataMember]
        public Vector3 InletDirection { get; set; } = null;       

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Network Clone(Source s = null)
        {
            var n = new Network()
            {
                Source = s ?? this.Source.Clone(),
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
                T.Add(new Transient() { Position = t.Position.Copy() });
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
                    c.End = t.Clone();
                    c.End.Parent = l;
                    c.End.Network = n;
                    l.End = c.End;
                    break;
                case Bifurcation s:
                    c.End = new Bifurcation()
                    {
                        Parent = l,
                        Position = s.Position.Copy(),
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
            c.Initialize(f);
            return c;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AxialBounds GetAxialBounds()
        {
            return this.Root.GlobalBounds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        public void Query(AxialBounds query, Action<Branch> action)
        {
            BranchQuery(query, action, this.Root);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        /// <param name="branch"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        public void Query(AxialBounds query, Action<Segment> action)
        {
            SegmentQuery(query, action, this.Root);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        /// <param name="branch"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Segment> GetEnumerator()
        {
            return this.Segments.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            yield break;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator<Branch> IEnumerable<Branch>.GetEnumerator()
        {
            return this.Branches.GetEnumerator();
        }

        /// <summary>
        /// Stack based iteration.
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<IMobileNode> MobileNodes
        {
            get
            {
                foreach (var node in this.Nodes)
                {
                    if (node is IMobileNode mobile)
                    {
                        yield return mobile;
                    }
                }
            }
        }

        /// <summary>
        /// Stack-based. See <see cref="Terminal.ForDownstream(Branch, Action{Terminal})"/>, <see cref="Terminal.GetDownstream(Branch, int)"/>
        /// for a recursive implementation.
        /// </summary>
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

        /// <summary>
        /// Visits each downstream branch, spawining new tasks until a depth of <paramref name="splitDepth"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="splitDepth"></param>
        /// <returns></returns>
        public async Task VisitAsync(Action<Branch> action, int splitDepth)
        {
            await VisitAsync(this.Root, action, splitDepth);
        }

        /// <summary>
        /// Similar to <see cref="VisitAsync(Action{Branch}, int)"/> but from any starting branch.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="action"></param>
        /// <param name="splitDepth"></param>
        /// <returns></returns>
        public static async Task VisitAsync(Branch branch, Action<Branch> action, int splitDepth)
        {
            if (splitDepth > 0)
            {
                action(branch);
                await branch.Children.RunAsync(child => VisitAsync(child, action, splitDepth - 1));
            }
            else
            {
                action(branch);
                branch.End.ForEach(action);
            }
        }
    }
}
