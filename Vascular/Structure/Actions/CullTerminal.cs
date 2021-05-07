using System;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Wrapper for <see cref="Topology.CullTerminal(Terminal, bool)"/>.
    /// </summary>
    public class CullTerminal : TopologyAction
    {
        private readonly Terminal t;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public CullTerminal(Terminal t)
        {
            this.t = t;
        }

        /// <summary>
        /// 
        /// </summary>
        public Action<Terminal> OnCull { get; set; }

        /// <inheritdoc/>
        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
            this.OnCull?.Invoke(t);
            if (Topology.CullTerminal(t) is Transient T)
            {
                if (propagateLogical)
                {
                    T.Parent.Branch.PropagateLogicalUpstream();
                    if (propagatePhysical)
                    {
                        T.UpdatePhysicalAndPropagate();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override bool IsPermissible()
        {
            return t.Parent != null && !(t.Upstream.Start is Source);
        }
    }
}
