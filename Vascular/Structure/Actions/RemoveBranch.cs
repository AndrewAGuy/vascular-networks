using System;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Wrapper for <see cref="Topology.RemoveBranch(Branch, bool, bool, bool, bool)"/>.
    /// </summary>
    public class RemoveBranch : BranchAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        public RemoveBranch(Branch a) : base(a, a)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public Action<Terminal> OnCull { get; set; }

        /// <inheritdoc/>
        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
            if (this.OnCull != null)
            {
                Terminal.ForDownstream(a, this.OnCull);
            }
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

        /// <inheritdoc/>
        public override bool Update()
        {
            a = a.CurrentTopologicallyValid;
            return a != null;
        }

        /// <inheritdoc/>
        public override bool IsValid()
        {
            return a.IsTopologicallyValid;
        }

        /// <inheritdoc/>
        public override bool Intersects(BranchAction other)
        {
            return ReferenceEquals(a, other.A)
                || ReferenceEquals(a, other.B);
        }

        /// <inheritdoc/>
        public override bool IsPermissable()
        {
            return a.IsRooted;
        }
    }
}
