using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vascular.Geometry.Lattices.Transformed;

namespace Vascular.Geometry.Lattices
{
    [DataContract]
    [KnownType(typeof(BodyCentredCubicLattice))]
    [KnownType(typeof(CubeLattice))]
    [KnownType(typeof(CuboidLattice))]
    [KnownType(typeof(HexagonalPrismLattice))]
    [KnownType(typeof(TetrahedronLattice))]
    [KnownType(typeof(OffsetLattice))]
    [KnownType(typeof(RefinedLattice))]
    [KnownType(typeof(RotatedLattice))]
    [KnownType(typeof(RotatedOffsetLattice))]
    public abstract class Lattice
    {
        public abstract Vector3 ToSpace(Vector3 u);
        public abstract Vector3 ToBasis(Vector3 v);
        public abstract Vector3 ClosestVectorBasis(Vector3 v);

        [DataMember]
        protected Matrix3 basis;
        [DataMember]
        protected double determinant;
        [DataMember]
        protected VoronoiCell voronoiCell;
        public Matrix3 Basis
        {
            get => basis;
            protected set
            {
                basis = value;
                determinant = basis.Determinant;
            }
        }
        public double Determinant => determinant;
        public VoronoiCell VoronoiCell => voronoiCell;

        public Vector3 NearestBasisRounding(Vector3 v)
        {
            return ToBasis(v).NearestIntegral();
        }

        public Vector3 NearestBasisConnection(Vector3 v, Vector3[] C)
        {
            var uR = ToBasis(v).NearestIntegral();
            var d2 = Vector3.DistanceSquared(v, ToSpace(uR));
            var uT = uR;
            foreach (var c in C)
            {
                var uc = uR + c;
                var dc = Vector3.DistanceSquared(v, ToSpace(uc));
                if (dc < d2)
                {
                    d2 = dc;
                    uT = uc;
                }
            }
            return uT;
        }

        public Vector3 NearestBasisVoronoi(Vector3 v, int i)
        {
            var u = ToBasis(v).NearestIntegral();
            var a = v - ToSpace(u);
            var m = voronoiCell.MostViolatedConnection(a);
            var n = 0;
            while (m != null && ++n <= i)
            {
                u += m;
                a = v - ToSpace(u);
                m = voronoiCell.MostViolatedConnection(a);
            }
            return u;
        }
    }
}
