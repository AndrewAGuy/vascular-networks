using System;
using Vascular.Structure.Diagnostics;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Base type for topology actions that impact multiple branches.
    /// </summary>
    public abstract class BranchAction : TopologyAction
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public BranchAction(Branch a, Branch b)
        {
            this.a = a;
            this.b = b;
        }

        /// <summary>
        ///
        /// </summary>
        protected Branch a;

        /// <summary>
        ///
        /// </summary>
        protected Branch b;

        /// <summary>
        ///
        /// </summary>
        public Branch A => a;

        /// <summary>
        ///
        /// </summary>
        public Branch B => b;

        /// <summary>
        /// Sets the branches referenced to <see cref="Branch.CurrentTopologicallyValid"/>, then checks for collapse or complete removal.
        /// </summary>
        /// <returns></returns>
        public virtual bool Update()
        {
            a = a.CurrentTopologicallyValid!;
            b = b.CurrentTopologicallyValid!;
            return !ReferenceEquals(a, b)
                && a != null
                && b != null; // In case anything has been completely removed
        }

        /// <summary>
        /// Makes sure that the branches referenced haven't been removed.
        /// </summary>
        /// <returns></returns>
        public virtual bool IsValid()
        {
            return a.IsTopologicallyValid
                && b.IsTopologicallyValid;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Intersects(BranchAction other)
        {
            return ReferenceEquals(a, other.a)
                || ReferenceEquals(b, other.b)
                || ReferenceEquals(a, other.b)
                || ReferenceEquals(b, other.a);
        }

        /// <summary>
        /// Undoes the action - requires actions to maintain a reference to the exact branches, otherwise all
        /// actions would need to be updated.
        /// </summary>
        /// <param name="propagateLogical"></param>
        /// <param name="propagatePhysical"></param>
        public abstract void Reverse(bool propagateLogical = true, bool propagatePhysical = false);

        /// <summary>
        ///
        /// </summary>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static Func<BranchAction, bool> Wrap(Func<Branch, Branch, bool> pred)
        {
            return action => pred(action.A, action.B);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        /// <param name="clone"></param>
        /// <returns></returns>
        public static BranchAction? TransferToClone(BranchAction action, Network clone)
        {
            var a = Address.Navigate(clone.Root, Address.Get(action.A));
            var b = Address.Navigate(clone.Root, Address.Get(action.B));
            return action switch
            {
                SwapEnds => new SwapEnds(a, b),
                MoveBifurcation mb => new MoveBifurcation(a, b) { Position = mb.Position },
                RemoveBranch rb => new RemoveBranch(a) { OnCull = rb.OnCull },
                _ => null
            };
        }
    }
}
