using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Structure.Actions
{
    public abstract class TopologyAction
    {
        public abstract void Execute(bool propagateLogical = true, bool propagatePhysical = false);
        public abstract bool IsPermissable();
    }
}
