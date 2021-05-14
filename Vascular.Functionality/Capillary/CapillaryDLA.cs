using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Graphs;
using Vascular.Structure;

namespace Vascular.Functionality.Capillary
{
    class CapillaryDLA : Continuous<Vertex, Edge>
    {
        public override Graph<Vertex, Edge> GenerateChunk(AxialBounds bounds)
        {
            throw new NotImplementedException();
        }

        public override bool IsIntersectionPermitted(Segment segment, double overlap)
        {
            throw new NotImplementedException();
        }

        public override double GetRadius(Edge e)
        {
            throw new NotImplementedException();
        }

        protected override void StitchChunks(Graph<Vertex, Edge> existing, Graph<Vertex, Edge> adding)
        {
            throw new NotImplementedException();
        }
    }
}
