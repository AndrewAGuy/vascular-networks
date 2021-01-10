using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Vascular.Geometry.Lattices
{
    [DataContract]
    public class VoronoiCell
    {
        [DataContract]
        private struct HalfSpace
        {
            [DataMember]
            public double x, y, z, d;
            public double Distance(Vector3 v)
            {
                return v.x * x + v.y * y + v.z * z;
            }
        }
        [DataMember]
        private readonly HalfSpace[] halfSpaces;

        public VoronoiCell(Vector3[] basisConnections, Matrix3 basis)
        {
            this.Connections = basisConnections;
            halfSpaces = new HalfSpace[basisConnections.Length];
            for (var i = 0; i < basisConnections.Length; ++i)
            {
                var v = basis * basisConnections[i];
                var mv = v.Length;
                halfSpaces[i].x = v.x / mv;
                halfSpaces[i].y = v.y / mv;
                halfSpaces[i].z = v.z / mv;
                halfSpaces[i].d = mv * 0.5;
            }
        }

        [DataMember]
        public Vector3[] Connections { get; }

        public Vector3 MostViolatedConnection(Vector3 v)
        {
            Vector3 mostViolated = null;
            var greatestViolation = 0.0;
            for (var i = 0; i < halfSpaces.Length; ++i)
            {
                var distance = halfSpaces[i].Distance(v);
                if (distance > halfSpaces[i].d)
                {
                    var violation = distance - halfSpaces[i].d;
                    if (violation > greatestViolation)
                    {
                        greatestViolation = violation;
                        mostViolated = this.Connections[i];
                    }
                }
            }
            return mostViolated;
        }
    }
}
