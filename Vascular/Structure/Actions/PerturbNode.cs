using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Structure.Actions
{
    public class PerturbNode : GeometryAction
    {
        private readonly IMobileNode n;
        private readonly Vector3 p;
        private readonly bool r;
        public PerturbNode(IMobileNode n, Vector3 p, bool r = true)
        {
            this.n = n;
            this.p = p;
            this.r = r;
        }

        public override void Execute(bool propagate = false)
        {
            n.Position = r ? n.Position + p : p;
            if (propagate)
            {
                n.UpdatePhysicalAndPropagate();
            }
        }
    }
}
