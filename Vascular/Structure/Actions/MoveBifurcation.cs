using System;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Wrapper for <see cref="Topology.MoveBifurcation(Branch, Segment)"/>
    /// </summary>
    public class MoveBifurcation : BranchAction
    {
        /// <summary>
        /// Moves <paramref name="moving"/> to bifurcate from <paramref name="target"/>.
        /// </summary>
        /// <param name="moving"></param>
        /// <param name="target"></param>
        public MoveBifurcation(Branch moving, Branch target) : base(moving, target)
        {
        }

        /// <summary>
        /// Where to place the newly created bifurcation. If null, uses <see cref="Bifurcation.WeightedMean(Func{Branch, double})"/>
        /// with unit weighting.
        /// </summary>
        public Func<Bifurcation, Vector3>? Position { get; set; } = null;

        /// <inheritdoc/>
        public override void Execute(bool propagateLogical, bool propagatePhysical)
        {
            GetReverseData();
            var (n, t) = Topology.MoveBifurcation(a, b.Segments[0]);
            if (n != null)
            {
                if (propagateLogical)
                {
                    t.Parent!.Branch.PropagateLogicalUpstream();
                    n.UpdateLogicalAndPropagate();
                    n.Position = this.Position?.Invoke(n) ?? n.WeightedMean(b => 1.0);
                    if (propagatePhysical && t is IMobileNode mn)
                    {
                        mn.UpdatePhysicalAndPropagate();
                        n.UpdatePhysicalAndPropagate();
                    }
                }
                else
                {
                    n.Position = this.Position?.Invoke(n) ?? n.WeightedMean(b => 1.0);
                }
            }
        }

        /// <inheritdoc/>
        public override bool IsPermissible()
        {
            return !a.IsStrictAncestorOf(b) // Creates a loop
                && !a.IsSiblingOf(b)        // Waste of time
                && !b.IsParentOf(a)         //
                && b.IsRooted;              // Cannot merge onto a culled section
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is MoveBifurcation other && a == other.a && b == other.b;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(a, b);
        }

        private Branch? sibling;
        private int index;
        private Vector3 position = Vector3.INVALID;

        private void GetReverseData()
        {
            sibling = a.FirstSibling;
            index = a.IndexInParent;
            position = a.Start.Position;
        }

        /// <inheritdoc/>
        public override void Reverse(bool propagateLogical = true, bool propagatePhysical = false)
        {
            // Need to reconstruct exactly as it was. We have created 2 new branches and lost 2 old.

            // Start by removing the bifurcation: moved branch points to same end node, but is not actually valid.
            var aa = a.CurrentTopologicallyValid!;
            var tr = Topology.CullBranch(aa.Start, aa.IndexInParent, null); //Topology.RemoveBranch(a.CurrentTopologicallyValid!, true, false, false, false)!;
            tr.Parent!.Branch.Reset();

            var bf = new Bifurcation()
            {
                Position = position,
                Network = a.End.Network
            };

            // Sibling was consumed by parent, rewire to end at bifurcation and reset
            var p = sibling!.CurrentTopologicallyValid!;
            p.End = bf;
            p.Reset();

            // Ensure that errors aren't introduced by segments shared between branches
            var ss = sibling.Segments[0];
            ss.End = sibling.End;
            ss.Start = bf;
            bf.Children[1 - index] = ss;
            sibling.End.Parent = ss;
            sibling.Start = bf;
            sibling.Initialize(ss);

            var sm = a.Segments[0];
            sm.End = a.End;
            sm.Start = bf;
            bf.Children[index] = sm;
            a.End.Parent = sm;
            a.Start = bf;
            a.Initialize(sm);

            bf.UpdateDownstream();

            // Finish with data propagation
            if (propagateLogical)
            {
                tr.Parent.Branch.PropagateLogicalUpstream();
                bf.UpdateLogicalAndPropagate();
                if (propagatePhysical && tr is IMobileNode mn)
                {
                    mn.UpdatePhysicalAndPropagate();
                    bf.UpdatePhysicalAndPropagate();
                }
            }
        }
    }
}
