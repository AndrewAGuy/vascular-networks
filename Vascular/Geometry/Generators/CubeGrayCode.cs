using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Vascular.Geometry.Generators
{
    [DataContract]
    public class CubeGrayCode : IVector3Generator
    {
        private static readonly Vector3[] DIRECTIONS = new Vector3[8] {
            new Vector3(+1, +1, +1),
            new Vector3(-1, +1, +1),
            new Vector3(-1, +1, -1),
            new Vector3(-1, -1, -1),
            new Vector3(-1, -1, +1),
            new Vector3(+1, -1, +1),
            new Vector3(+1, -1, -1),
            new Vector3(+1, +1, -1),
            };

        [DataMember]
        private uint index = uint.MaxValue;

        public Vector3 NextVector3()
        {
            return DIRECTIONS[++index % 8];
        }

        public Vector3 GenerateArbitraryNormal(Vector3 v, double tol2 = 1.0e-10)
        {
            // Arbitrary axis chosen, remove component in v and see what remains.
            // If nothing remains, next axis will be suitable since Gray code ordering.
            var dir = DIRECTIONS[++index % 8];
            var nor = dir - dir * v / (v * v) * v;
            var mag2 = nor.LengthSquared;
            return mag2 > tol2 ? nor / Math.Sqrt(mag2)
                : v.LengthSquared > tol2 ? GenerateArbitraryNormal(v, tol2)
                : throw new PhysicalValueException("Vector is too small to meaningfully assign normal to.");
        }
    }
}
