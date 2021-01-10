using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Vascular.Geometry.Lattices
{
    [DataContract]
    public class BodyCentredCubicLattice : Lattice
    {
        [DataMember]
        private readonly Matrix3 inverse;

        public BodyCentredCubicLattice(double length)
        {
            var b0 = new Vector3(length, 0, 0);
            var b1 = new Vector3(0, length, 0);
            var b2 = new Vector3(length / 2.0);
            basis = Matrix3.FromColumns(b0, b1, b2);
            inverse = basis.Inverse();
            voronoiCell = new VoronoiCell(Connectivity.BodyCentredCubic, basis);
        }

        public override Vector3 ToSpace(Vector3 u)
        {
            return basis * u;
        }

        public override Vector3 ToBasis(Vector3 v)
        {
            return inverse * v;
        }

        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisVoronoi(v, 3);
        }
    }
}
