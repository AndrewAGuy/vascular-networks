using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    public class CullTerminal : TopologyAction
    {
        private readonly Terminal t;
        public CullTerminal(Terminal t)
        {
            this.t = t;
        }

        public override void Execute(bool propagateLogical = true, bool propagatePhysical = false)
        {
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

        public override bool IsPermissable()
        {
            return t.Parent != null && !(t.Upstream.Start is Source);
        }
    }
}
