using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Geometry.Graphs
{
    public class Vertex
    {
        public Vertex(Vector3 p)
        {
            P = p;
        }

        public Vector3 P;

        public LinkedList<Edge> E = new LinkedList<Edge>();
    }
}
