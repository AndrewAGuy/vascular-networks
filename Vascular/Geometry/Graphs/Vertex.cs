using System.Collections.Generic;

namespace Vascular.Geometry.Graphs
{
    /// <summary>
    /// 
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public Vertex(Vector3 p)
        {
            P = p;
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 P;

        /// <summary>
        /// 
        /// </summary>
        public LinkedList<Edge> E = new LinkedList<Edge>();
    }
}
