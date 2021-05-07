using System.Collections.Generic;
using System.Runtime.Serialization;
using Vascular.Geometry.Bounds;

namespace Vascular.Geometry.Triangulation
{
    /// <summary>
    /// Wound A-B-C, hence naming edge CA rather than AC.
    /// </summary>
    [DataContract]
    public class Triangle : IAxialBoundable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="ab"></param>
        /// <param name="bc"></param>
        /// <param name="ca"></param>
        /// <param name="n"></param>
        public Triangle(Vertex a, Vertex b, Vertex c, Edge ab, Edge bc, Edge ca, Vector3 n)
        {
            (A, B, C, AB, BC, CA, N) = (a, b, c, ab, bc, ca, n);
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public Vertex A, B, C;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public Edge AB, BC, CA;

        /// <summary>
        /// Normal.
        /// </summary>
        [DataMember]
        public Vector3 N;

        /// <summary>
        /// For removing from the mesh.
        /// </summary>
        [DataMember]
        public LinkedListNode<Triangle> Node;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        public void EdgeNeighbours(ICollection<Triangle> n)
        {
            foreach (var t in AB.T)
            {
                if (t != this)
                {
                    n.Add(t);
                }
            }
            foreach (var t in BC.T)
            {
                if (t != this)
                {
                    n.Add(t);
                }
            }
            foreach (var t in CA.T)
            {
                if (t != this)
                {
                    n.Add(t);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Edge Opposite(Vertex v)
        {
            return v == A ? BC : v == B ? CA : v == C ? AB : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Vertex Opposite(Edge e)
        {
            return ReferenceEquals(e, AB) ? C : ReferenceEquals(e, BC) ? A : ReferenceEquals(e, CA) ? B : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AxialBounds Bounds()
        {
            return new AxialBounds(A.P).Append(B.P).Append(C.P);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsDegenerate => A == B || B == C || C == A;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool Contains(Vertex v)
        {
            return A == v || B == v || C == v;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool Contains(Edge e)
        {
            return AB.Equals(e) || BC.Equals(e) || CA.Equals(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool ReferenceContains(Edge e)
        {
            return ReferenceEquals(AB, e) || ReferenceEquals(BC, e) || ReferenceEquals(CA, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public Vertex Extremum(Vector3 dir)
        {
            var v0 = dir * A.P;
            var v1 = dir * B.P;
            var v2 = dir * C.P;
            return v0 > v1 ? v0 > v2 ? A : C : v1 > v2 ? B : C;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vertex Closest(Vector3 pos)
        {
            var dA = Vector3.DistanceSquared(pos, A.P);
            var dB = Vector3.DistanceSquared(pos, B.P);
            var dC = Vector3.DistanceSquared(pos, C.P);
            return dA < dB ? dA < dC ? A : C : dB < dC ? B : C;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AxialBounds GetAxialBounds()
        {
            return new AxialBounds(A.P).Append(B.P).Append(C.P);
        }
    }
}
