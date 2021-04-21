using System;
using System.Collections.Generic;
using System.Linq;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Greedily executes from a series of candidate actions until no more valid actions remain.
    /// </summary>
    public class TopologyExecutor
    {
        private IEnumerable<BranchAction> actions;

        /// <summary>
        /// Higher values are executed first.
        /// </summary>
        public Func<Branch, Branch, double> Priority { get; set; } = (a, b) => 0.0;

        /// <summary>
        /// Whether an action may be taken.
        /// </summary>
        public Func<Branch, Branch, bool> Permissable { get; set; } = (a, b) => true;

        /// <summary>
        /// Propagate topological derived properties upstream on making changes.
        /// </summary>
        public bool PropagateLogical { get; set; } = true;

        /// <summary>
        /// Propagate geometric derived properties upstream on making changes.
        /// </summary>
        public bool PropagatePhysical { get; set; } = false;
        
        /// <summary>
        /// At the end of each iteration, update each action to ensure topological validity.
        /// </summary>
        public bool TryUpdate { get; set; } = true;
        
        /// <summary>
        /// Tested after each iteration where valid candidates remain, if returns false then terminates.
        /// </summary>
        public Func<bool> ContinuationPredicate { get; set; } = () => true;

        private bool Iterate()
        {
            if (!actions.MinSuitable(
                t => -this.Priority(t.A, t.B),
                t => t.IsPermissible() && this.Permissable(t.A, t.B),
                out var a, out var v))
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actions"></param>
        /// <returns>The number of steps taken.</returns>
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
