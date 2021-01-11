using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Structure.Actions
{
    public class RemoveBranch : BranchAction
    {
        public RemoveBranch(Branch a, bool markDownstream = true) : base(a, a)
        {
            this.markDownstream = markDownstream;
        }

        private bool markDownstream;

        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
            var transient = Topology.RemoveBranch(a, true, markDownstream);

        }

        public override bool IsPermissable()
        {
            return true;
        }
    }
}
