using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Vascular.Geometry.Lattices.Transformed
{
    [DataContract]
    public class RotatedOffsetLattice : Lattice
    {
        [DataMember]
        private readonly Lattice lattice;
        [DataMember]
        private readonly Matrix3 rotation;
        [DataMember]
        private readonly Matrix3 inverse;
        [DataMember]
        private readonly Vector3 offset;

        public RotatedOffsetLattice(Lattice lattice, Matrix3 rotation, Vector3 offset)
        {
            this.lattice = lattice;
            this.rotation = rotation;
            inverse = rotation.Inverse();
            this.offset = offset;
            this.Basis = rotation * lattice.Basis;
            voronoiCell = new VoronoiCell(lattice.VoronoiCell.Connections, this.Basis);
        }

        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return lattice.ClosestVectorBasis(inverse * (v - offset));
        }

        public override Vector3 ToBasis(Vector3 v)
        {
            return lattice.ToBasis(inverse * (v - offset));
        }

        public override Vector3 ToSpace(Vector3 u)
        {
            return rotation * lattice.ToSpace(u) + offset;
        }
    }
}
