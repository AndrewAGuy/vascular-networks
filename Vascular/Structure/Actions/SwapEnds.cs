using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure.Actions
{
    public class SwapEnds : BranchAction
    {
        public SwapEnds(Branch a, Branch b) : base(a, b)
        {
        }

        public override void Execute(bool propagateLogical, bool propagatePhysical)
        {
            Topology.SwapEnds(a, b);
            if (propagateLogical)
            {
                a.End.PropagateLogicalUpstream();
                b.End.PropagateLogicalUpstream();
                if (propagatePhysical)
                {
                    a.UpdateLengths();
                    a.UpdatePhysicalLocal();
                    a.PropagatePhysicalUpstream();
                    b.UpdateLengths();
                    b.UpdatePhysicalLocal();
                    b.PropagatePhysicalUpstream();
                }
            }
        }

        public override bool IsPermissable()
        {
            return !a.IsStrictAncestorOf(b) // Loop avoidance
                && !b.IsStrictAncestorOf(a) //
                && !a.IsSiblingOf(b)        // Waste of time
                && a.IsRooted               // Cannot send subtree into the void
                && b.IsRooted;              // 
        }

        public override bool Equals(object obj)
        {
            return obj is SwapEnds o && (a == o.a && b == o.b || a == o.b && b == o.a);
        }

        public override int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode();
        }
    }
}
