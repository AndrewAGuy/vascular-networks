using System.Collections.Generic;

namespace Vascular.Geometry.Graphs
{
    /// <summary>
    ///
    /// </summary>
    public class Vertex<TVertex, TEdge>
        where TVertex : Vertex<TVertex, TEdge>
        where TEdge : Edge<TVertex, TEdge>
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
        public Vertex()
        {
            P = null!;
        }

        /// <summary>
        ///
        /// </summary>
        public Vector3 P;

        /// <summary>
        ///
        /// </summary>
        public LinkedList<TEdge> E = new();
    }
}
