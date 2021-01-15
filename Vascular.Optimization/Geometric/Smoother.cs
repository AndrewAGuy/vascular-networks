using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    public class Smoother
    {
        public Func<Terminal, Vector3> TerminalDirection { get; set; }
        public Func<Source, Vector3> SourceDirection { get; set; }

        private void MoveTransients(Branch branch, Vector3 inletDirection, Vector3 outletDirection)
        {

        }
    }
}
