namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Wrapper for <see cref="Topology.SwapEnds(Branch, Branch)"/>.
    /// </summary>
    public class SwapEnds : BranchAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public SwapEnds(Branch a, Branch b) : base(a, b)
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override bool IsPermissible()
        {
            return !a.IsStrictAncestorOf(b) // Loop avoidance
                && !b.IsStrictAncestorOf(a) //
                && !a.IsSiblingOf(b)        // Waste of time
                && a.IsRooted               // Cannot send subtree into the void
                && b.IsRooted;              // 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is SwapEnds o && (a == o.a && b == o.b || a == o.b && b == o.a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode();
        }
    }
}
