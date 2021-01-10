using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry;

namespace Vascular.Geometry.Triangulation
{
    [DataContract]
    public class Edge : IEquatable<Edge>
    {
        public Edge(Vertex s, Vertex e)
        {
            (S, E) = (s, e);
        }

        [DataMember]
        public Vertex S, E;
        [DataMember]
        public LinkedList<Triangle> T;

        public Vector3 Direction => E.P - S.P;

        public double Length => Vector3.Distance(S.P, E.P);

        public bool IsConcave()
        {
            var t1n = T.First.Value.N;
            var t2p = T.Last.Value.Opposite(this);
            return t1n * (t2p.P - S.P) > 0;
        }

        public bool IsDegenerate
        {
            get
            {
                return S == E;
            }
        }

        public Vector3 GetNormal()
        {
            var v = new Vector3();
            foreach (var t in T)
            {
                v += t.N;
            }
            return (v / T.Count).Normalize();
        }

        public bool CheckNormalConsistency()
        {
            if (T.Count == 1)
            {
                return true;
            }
            else if (T.Count != 2)
            {
                return false;
            }
            // Are the two triangles touching this edge consistent in their normals?
            // If so, the winding direction of this edge will be opposite in each triangle
            // If vector from start to other vertex has positive component of n x d, then positively wound
            var d = this.Direction;
            var t1 = T.First.Value;
            var v1 = t1.Opposite(this).P - S.P;
            var nd1 = t1.N ^ d;
            var p1 = v1 * nd1 > 0.0;
            var t2 = T.Last.Value;
            var v2 = t2.Opposite(this).P - S.P;
            var nd2 = t2.N ^ d;
            var p2 = v2 * nd2 > 0.0;
            return p1 != p2;
        }

        public Edge CorrectWindingIn(Triangle t)
        {
            return t.N * (this.Direction ^ (t.Opposite(this).P - S.P)) > 0 ? this : this.Reverse;
        }

        public Edge Reverse
        {
            get
            {
                return new Edge(E, S);
            }
        }

        public Vertex Other(Vertex v)
        {
            return v == S ? E : S;
        }

        public Vertex OtherSafe(Vertex v)
        {
            return v == S ? E : v == E ? S : null;
        }

        public Triangle Other(Triangle t)
        {
            return T.First.Value == t ? T.Last.Value : T.First.Value;
        }

        public Triangle OtherSafe(Triangle t)
        {
            if (T.Count == 2)
            {
                if (T.First.Value == t)
                {
                    return T.Last.Value;
                }
                else if (T.Last.Value == t)
                {
                    return T.First.Value;
                }
            }
            return null;
        }

        public bool Equals(Edge o)
        {
            return (E == o.E && S == o.S) || (E == o.S && S == o.E);
        }

        public override bool Equals(object obj)
        {
            return obj is Edge e && Equals(e);
        }

        public override int GetHashCode()
        {
            return S.GetHashCode() ^ E.GetHashCode();
        }

        public bool CanCollapse
        {
            get
            {
                var fan = S.UnorderedFan;
                var joint = 0;
                foreach (var v in E.UnorderedFan)
                {
                    if (fan.Contains(v))
                    {
                        ++joint;
                    }
                }
                return joint == 2;
            }
        }

        public Plane3 Midplane
        {
            get
            {
                var ed = this.Direction;
                var mn = T.First.Value.N + T.Last.Value.N;
                var pn = (ed ^ mn).Normalize();
                return new Plane3(pn, pn * S.P);
            }
        }

        public double DihedralAngleCosine => T.First.Value.N * T.Last.Value.N;
    }
}
