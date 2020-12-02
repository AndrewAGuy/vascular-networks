using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Structure.Actions
{
    public class InsertTransient : GeometryAction
    {
        private readonly Segment s;
        private readonly Vector3 p;
        public InsertTransient(Segment s, Vector3 p)
        {
            this.s = s;
            this.p = p;
        }

        public override void Execute(bool propagate = false)
        {
            var t = Topology.InsertTransient(s);
            t.Position = p;
            if (propagate)
            {
                t.UpdatePhysicalAndPropagate();
            }
        }
    }
}
