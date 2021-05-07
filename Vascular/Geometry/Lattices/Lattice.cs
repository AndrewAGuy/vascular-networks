using System.Runtime.Serialization;
using Vascular.Geometry.Lattices.Transformed;

namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// The base type for all lattices. Defines a mapping between integral vectors and real space.
    /// </summary>
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
        /// <summary>
        /// The forwards transform from integral coordinates.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public abstract Vector3 ToSpace(Vector3 u);

        /// <summary>
        /// The backwards transform to integral + fractional coordinates.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public abstract Vector3 ToBasis(Vector3 v);

        /// <summary>
        /// The integral coordinates associated with the closest lattice vector to <paramref name="v"/>.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public abstract Vector3 ClosestVectorBasis(Vector3 v);

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        protected Matrix3 basis;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        protected double determinant;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        protected VoronoiCell voronoiCell;

        /// <summary>
        /// 
        /// </summary>
        public Matrix3 Basis
        {
            get => basis;
            protected set
            {
                basis = value;
                determinant = basis.Determinant;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Determinant => determinant;

        /// <summary>
        /// 
        /// </summary>
        public VoronoiCell VoronoiCell => voronoiCell;

        /// <summary>
        /// For lattices with right angles.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector3 NearestBasisRounding(Vector3 v)
        {
            return ToBasis(v).NearestIntegral();
        }

        /// <summary>
        /// Search a single connection pattern to <see cref="NearestBasisRounding(Vector3)"/> and return the closest.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="C"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Starting from <see cref="NearestBasisRounding(Vector3)"/>, walk the Voronoi cell <paramref name="i"/> times
        /// at most until no more violation. Can behave weirdly at boundaries, hence the limit.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="i"></param>
        /// <returns></returns>
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
