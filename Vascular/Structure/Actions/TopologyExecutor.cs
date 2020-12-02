using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vascular.Structure.Actions
{
    public class TopologyExecutor
    {
        private IEnumerable<BranchAction> actions;

        public Func<Branch, Branch, double> Priority { get; set; } = (a, b) => 0.0;
        public Func<Branch, Branch, bool> Permissable { get; set; } = (a, b) => true;
        public bool PropagateLogical { get; set; } = true;
        public bool PropagatePhysical { get; set; } = false;
        public bool TryUpdate { get; set; } = true;
        public Func<bool> ContinuationPredicate { get; set; } = () => true;

        private bool Iterate()
        {
            var a = actions.MinSuitable(
                t => 1.0 / (1.0 + this.Priority(t.A, t.B)),
                t => t.IsPermissable() && this.Permissable(t.A, t.B));
            if (a == null)
            {
                return false;
            }
            a.Execute(this.PropagateLogical, this.PropagatePhysical);
            var p = this.TryUpdate
                ? new Func<BranchAction, bool>(t => !t.Intersects(a) && t.Update())
                : new Func<BranchAction, bool>(t => !t.Intersects(a) && t.IsValid());
            actions = actions.Where(p);
            return true;
        }

        public int Iterate(IEnumerable<BranchAction> actions)
        {
            this.actions = actions;
            var taken = 0;
            while (Iterate())
            {
                ++taken;
                if (!this.ContinuationPredicate())
                {
                    break;
                }
            }
            return taken;
        }
    }
}
