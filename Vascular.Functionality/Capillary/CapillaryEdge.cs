using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry.Graphs;

namespace Vascular.Functionality.Capillary
{
    /// <summary>
    /// 
    /// </summary>
    public class CapillaryEdge : Edge<Vertex<CapillaryEdge>, CapillaryEdge>
    {
        /// <summary>
        /// Set to true whenever an edge attaches to a major vessel.
        /// </summary>
        public bool IsAttached { get; set; }
    }
}
