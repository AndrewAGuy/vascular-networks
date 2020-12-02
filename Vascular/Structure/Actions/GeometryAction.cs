using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Structure.Actions
{
    public abstract class GeometryAction
    {
        public abstract void Execute(bool propagate = false);
    }
}
