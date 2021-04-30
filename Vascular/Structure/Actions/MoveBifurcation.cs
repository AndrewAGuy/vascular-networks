using System;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Wrapper for <see cref="Topology.MoveBifurcation(Branch, Branch)"/>.
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
        public Func<Bifurcation, Vector3> Position { get; set; } = null;

        /// <inheritdoc/>
        public override void Execute(bool propagateLogical, bool propagatePhysical)
        {
            var (t, n) = Topology.MoveBifurcation(a, b);
            if (n != null)
            {
                n.Position = this.Position?.Invoke(n) ?? n.WeightedMean(b => 1.0);
                if (propagateLogical)
                {
                    t.Parent.Branch.PropagateLogicalUpstream();
                    n.UpdateLogicalAndPropagate();
                    if (propagatePhysical)
                    {
                        t.UpdatePhysicalAndPropagate();
                        n.UpdatePhysicalAndPropagate();
                    }
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
        public override bool Equals(object obj)
        {
            return obj is MoveBifurcation other && a == other.a && b == other.b;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(a, b);
        }
    }
}
