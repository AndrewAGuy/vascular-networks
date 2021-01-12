using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    public class MoveBifurcation : BranchAction
    {
        public MoveBifurcation(Branch a, Branch b) : base(a, b)
        {
        }

        public Func<Bifurcation, Vector3> Position { get; set; } = null;

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

        public override bool IsPermissable()
        {
            return !a.IsStrictAncestorOf(b) // Creates a loop
                && !a.IsSiblingOf(b)        // Waste of time
                && !b.IsParentOf(a)         // 
                && b.IsRooted;              // Cannot merge onto a culled section
        }

        public override bool Equals(object obj)
        {
            return obj is MoveBifurcation other && a == other.a && b == other.b;
        }

        public override int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode();
        }
    }
}
