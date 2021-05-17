using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Graphs;
using Vascular.Structure;

namespace Vascular.Functionality.Capillary
{
    public abstract class CapillaryBase : Continuous<Vertex, Edge>
    {
        public double Radius { get; set; }

        public Network[] Networks { get; set; }

        public Func<Segment, bool> PermittedIntersection { get; set; } = s => true;

        public double MinOverlap { get; set; }

        public override double GetRadius(Edge e)
        {
            return this.Radius;
        }

        public override bool IsIntersectionPermitted(Segment segment, double overlap)
        {
            return this.PermittedIntersection(segment)
                && overlap >= this.MinOverlap;
        }
    }
}
