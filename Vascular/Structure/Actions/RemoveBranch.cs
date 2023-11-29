using System;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Wrapper for <see cref="Topology.CullBranch(BranchNode, int, Action{Terminal}?)"/>
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
        public Action<Terminal>? OnCull { get; set; }

        /// <inheritdoc/>
        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
            if (this.OnCull != null)
            {
                Terminal.ForDownstream(a, this.OnCull);
            }
            var transient = Topology.CullBranch(a.Start, a.IndexInParent, null); //Topology.RemoveBranch(a, true, true, false, true)!;
            if (propagateLogical)
            {
                transient.Parent!.Branch.PropagateLogicalUpstream();
                if (propagatePhysical && transient is IMobileNode mn)
                {
                    mn.UpdatePhysicalAndPropagate();
                }
            }
        }

        /// <inheritdoc/>
        public override bool Update()
        {
            a = a.CurrentTopologicallyValid!;
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
        public override bool IsPermissible()
        {
            return a.IsRooted;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return a.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is RemoveBranch r && a == r.a;
        }

        /// <summary>
        /// For now, this is treated as irreversible due to the lack of control over the <see cref="OnCull"/> action.
        /// </summary>
        /// <param name="propagateLogical"></param>
        /// <param name="propagatePhysical"></param>
        public override void Reverse(bool propagateLogical = true, bool propagatePhysical = false)
        {
            throw new NotImplementedException();
        }
    }
}
