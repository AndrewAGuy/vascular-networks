using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Structure.Actions
{
    public class RemoveBranch : BranchAction
    {
        public RemoveBranch(Branch a) : base(a, a)
        {
        }

        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
            var transient = Topology.RemoveBranch(a, true, true, false, true);
            if (propagateLogical)
            {
                transient.Child.Branch.PropagateLogicalUpstream();
                if (propagatePhysical)
                {
                    transient.UpdatePhysicalAndPropagate();
                }
            }
        }

        public override bool Update()
        {
            a = a.CurrentTopologicallyValid;
            return a != null;
        }

        public override bool IsValid()
        {
            return a.IsTopologicallyValid;
        }

        public override bool Intersects(BranchAction other)
        {
            return ReferenceEquals(a, other.A)
                || ReferenceEquals(a, other.B);
        }

        public override bool IsPermissable()
        {
            return a.IsRooted;
        }
    }
}
