using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Geometry.Triangulation
{
    [DataContract]
    public class Triangle : IAxialBoundable
    {
        public Triangle(Vertex a, Vertex b, Vertex c, Edge ab, Edge bc, Edge ca, Vector3 n)
        {
            (A, B, C, AB, BC, CA, N) = (a, b, c, ab, bc, ca, n);
        }

        [DataMember]
        public Vertex A, B, C;
        [DataMember]
        public Edge AB, BC, CA;
        [DataMember]
        public Vector3 N;
        [DataMember]
        public LinkedListNode<Triangle> Node;

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

        public Edge Opposite(Vertex v)
        {
            return v == A ? BC : v == B ? CA : v == C ? AB : null;
        }

        public Vertex Opposite(Edge e)
        {
            return ReferenceEquals(e, AB) ? C : ReferenceEquals(e, BC) ? A : ReferenceEquals(e, CA) ? B : null;
        }

        public AxialBounds Bounds()
        {
            return new AxialBounds(A.P).Append(B.P).Append(C.P);
        }

        public bool IsDegenerate => A == B || B == C || C == A;

        public bool Contains(Vertex v)
        {
            return A == v || B == v || C == v;
        }

        public bool Contains(Edge e)
        {
            return AB.Equals(e) || BC.Equals(e) || CA.Equals(e);
        }

        public bool ReferenceContains(Edge e)
        {
            return ReferenceEquals(AB, e) || ReferenceEquals(BC, e) || ReferenceEquals(CA, e);
        }

        public Vertex Extremum(Vector3 dir)
        {
            var v0 = dir * A.P;
            var v1 = dir * B.P;
            var v2 = dir * C.P;
            return v0 > v1 ? v0 > v2 ? A : C : v1 > v2 ? B : C;
        }

        public Vertex Closest(Vector3 pos)
        {
            var dA = Vector3.DistanceSquared(pos, A.P);
            var dB = Vector3.DistanceSquared(pos, B.P);
            var dC = Vector3.DistanceSquared(pos, C.P);
            return dA < dB ? dA < dC ? A : C : dB < dC ? B : C;
        }

        public AxialBounds GetAxialBounds()
        {
            return new AxialBounds(A.P).Append(B.P).Append(C.P);
        }
    }
}
