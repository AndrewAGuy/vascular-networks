using System.Collections.Generic;
using System.Linq;

namespace Vascular.Geometry.Triangulation
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
        /// Position.
        /// </summary>
        public Vector3 P;

        /// <summary>
        /// Edges.
        /// </summary>
        public LinkedList<Edge> E = new();

        /// <summary>
        /// Triangles.
        /// </summary>
        public LinkedList<Triangle> T = new();

        /// <summary>
        /// The vertices attached to this in order.
        /// </summary>
        public List<Vertex> Fan
        {
            get
            {
                var currentTriangle = T.First!.Value;
                var outerEdge = currentTriangle.Opposite(this)!.CorrectWindingIn(currentTriangle);
                var path = new List<Vertex>(E.Count);
                var first = outerEdge.S;
                path.Add(first);
                path.Add(outerEdge.E);
                while (true)
                {
                    var secondLast = path[^2];
                    var edgeInner = currentTriangle.Opposite(secondLast);
                    var nextTriangle = edgeInner!.Other(currentTriangle);
                    outerEdge = nextTriangle.Opposite(this)!.CorrectWindingIn(nextTriangle);
                    if (outerEdge.E == first)
                    {
                        break;
                    }
                    path.Add(outerEdge.E);
                    currentTriangle = nextTriangle;
                }
                return path;
            }
        }

        /// <summary>
        /// If a boundary vertex, the vertices attached to this as an open arc.
        /// </summary>
        public List<Vertex>? BoundaryFan
        {
            get
            {
                // Find which vertex is in the correct order, then wind around from there
                var e = E.Where(e => e.T.Count == 1).ToList();
                if (e.Count != 2)
                {
                    return null;
                }
                var ew0 = e[0].CorrectWindingIn(e[0].T.First!.Value);
                var ew1 = e[1].CorrectWindingIn(e[1].T.First!.Value);
                var b0 = ew0.S == this;
                var b1 = ew1.S == this;
                var P = new List<Vertex>(E.Count);
                Triangle TC = null!;
                if (b0 && !b1)
                {
                    P.Add(ew0.E);
                    TC = e[0].T.First!.Value;
                }
                else if (b1 && !b0)
                {
                    P.Add(ew1.E);
                    TC = e[1].T.First!.Value;
                }
                else
                {
                    return null;
                }

                // Now copied from fan method
                // Get opposite edge in triangle, then wind it (it will point towards this)
                while (true)
                {
                    var EO = TC.Opposite(P[^1])!;
                    if (EO.T.Count == 1)
                    {
                        P.Add(EO.CorrectWindingIn(TC).S);
                        return P;
                    }
                    var TN = EO.Other(TC);
                    P.Add(EO.CorrectWindingIn(TN).E);
                    TC = TN;
                }
            }
        }

        /// <summary>
        /// The vertices attached to this.
        /// </summary>
        public List<Vertex> UnorderedFan
        {
            get
            {
                var vertices = new List<Vertex>(E.Count);
                foreach (var e in E)
                {
                    vertices.Add(e.Other(this));
                }
                return vertices;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public List<Vertex>? FanFrom(Vertex start)
        {
            if (EdgeTo(start) is not Edge edge)
            {
                return null;
            }
            var fan = new List<Vertex>(E.Count) { start };
            Triangle currentTriangle;
            var lastVertex = start;
            if (edge.CorrectWindingIn(edge.T.First!.Value).E == start)
            {
                currentTriangle = edge.T.First.Value;
            }
            else if (edge.CorrectWindingIn(edge.T.Last!.Value).E == start)
            {
                currentTriangle = edge.T.Last.Value;
            }
            else
            {
                return null;
            }
            while (true)
            {
                var oppositeEdge = currentTriangle.Opposite(lastVertex)!;
                var nextTriangle = oppositeEdge.Other(currentTriangle);
                var nextVertex = oppositeEdge.CorrectWindingIn(nextTriangle).E;
                if (nextVertex == start)
                {
                    return fan;
                }
                fan.Add(nextVertex);
                currentTriangle = nextTriangle;
                lastVertex = nextVertex;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public Edge? EdgeTo(Vertex other)
        {
            foreach (var e in E)
            {
                if (e.Other(this) == other)
                {
                    return e;
                }
            }
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsConnected(Vertex other)
        {
            return E.Any(e => e.Other(this) == other);
        }

        /// <summary>
        ///
        /// </summary>
        public bool IsInterior
        {
            get
            {
                foreach (var e in E)
                {
                    if (e.T.Count != 2)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public bool VerifyEdgeCounts
        {
            get
            {
                foreach (var e in E)
                {
                    if (e.T.Count > 2)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool TriangleExists(Vertex u, Vertex v)
        {
            foreach (var t in T)
            {
                if (t.Contains(u) && t.Contains(v))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
