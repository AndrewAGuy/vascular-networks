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
        public Func<Branch, Branch, bool> Permissible { get; set; } = (a, b) => true;

        /// <summary>
        /// If present, overrides <see cref="Priority"/>. Lower values are better.
        /// </summary>
        public Func<BranchAction, double> Cost { get; set; }

        /// <summary>
        /// If present, overrides <see cref="Permissible"/>.
        /// </summary>
        public Func<BranchAction, bool> Predicate { get; set; }

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

        /// <summary>
        /// Executed before an action is taken. May be used for rapid updates of cached data if needed.
        /// </summary>
        public Action<BranchAction> BeforeExecute { get; set; }

        /// <summary>
        /// Executed after an action is taken. May be used for rapid updates of cached data if needed.
        /// </summary>
        public Action<BranchAction> AfterExecute { get; set; }

        private bool Iterate(Func<BranchAction, double> cost, Func<BranchAction, bool> predicate)
        {
            if (!actions.MinSuitable(cost, predicate, out var a, out var v))
            {
                return false;
            }
            this.BeforeExecute?.Invoke(a);
            a.Execute(this.PropagateLogical, this.PropagatePhysical);
            this.AfterExecute?.Invoke(a);
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
            var cost = this.Cost ?? (t => -this.Priority(t.A, t.B));
            var predicate = this.Predicate != null
                ? new Func<BranchAction, bool>(t => t.IsPermissible() && this.Predicate(t))
                : t => t.IsPermissible() && this.Permissible(t.A, t.B);
            while (Iterate(cost, predicate))
            {
                ++taken;
                if (!this.ContinuationPredicate())
                {
                    break;
                }
            }
            return taken;
        }

        /// <summary>
        /// Given a set of ordered actions, execute the first at each iteration.
        /// Filter the remaining actions afterwards.
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public int IterateOrdered(IEnumerable<BranchAction> actions)
        {
            var taken = 0;
            while (true)
            {
                actions = actions
                    .Where(t => t.IsPermissible())
                    .ToList(); // This seems to prevent a weird bug where .Any() returns true but .First() throws.
                if (!actions.Any())
                {
                    break;
                }

                if (this.IsAcceptable == null)
                {
                    var action = actions.First();
                    this.BeforeExecute?.Invoke(action);
                    action.Execute(this.PropagateLogical, this.PropagatePhysical);
                    this.AfterExecute?.Invoke(action);
                    var p = this.TryUpdate
                        ? new Func<BranchAction, bool>(t => !t.Intersects(action) && t.Update())
                        : new Func<BranchAction, bool>(t => !t.Intersects(action) && t.IsValid());
                    actions = actions.Where(p);
                }
                else
                {
                    (actions, taken) = TryAct(actions, taken);
                    if (!actions.Any())
                    {
                        break;
                    }
                }

                ++taken;
                if (!this.ContinuationPredicate())
                {
                    break;
                }
            }
            return taken;
        }

        private (IEnumerable<BranchAction>, int) TryAct(IEnumerable<BranchAction> actions, int taken)
        {
            var failed = 0;
            foreach (var action in actions)
            {
                this.BeforeExecute?.Invoke(action);
                action.Execute(this.PropagateLogical, this.PropagatePhysical);
                if (!this.IsAcceptable())
                {
                    failed++;
                    action.Reverse(this.PropagateLogical, this.PropagatePhysical);
                }
                else
                {
                    this.AfterExecute?.Invoke(action);
                    var p = this.TryUpdate
                        ? new Func<BranchAction, bool>(t => !t.Intersects(action) && t.Update())
                        : new Func<BranchAction, bool>(t => !t.Intersects(action) && t.IsValid());
                    return (actions.Skip(failed).Where(p), taken + 1);
                }
            }
            return (Enumerable.Empty<BranchAction>(), taken);
        }

        /// <summary>
        /// If set and used with <see cref="IterateOrdered(IEnumerable{BranchAction})"/>, attempts each action
        /// and then calls this. If this returns true, the action is kept, otherwise the action is reverted
        /// using <see cref="BranchAction.Reverse(bool, bool)"/> and discarded. At present, <see cref="RemoveBranch"/>
        /// is not supported in this mode - most costs would improve by removing branches though, so it would be
        /// unlikely to ever need reverting.
        /// </summary>
        public Func<bool> IsAcceptable { get; set; }

        /// <summary>
        /// Sets <see cref="ContinuationPredicate"/> to return false after <paramref name="n"/> iterations.
        /// </summary>
        /// <param name="n"></param>
        public void Limit(int n)
        {
            this.ContinuationPredicate = () => --n > 0;
        }
    }
}
