using System.Runtime.Serialization;

namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// A pair of cubic lattices with a half cell offset.
    /// </summary>
    public class BodyCentredCubicLattice : Lattice
    {
        private readonly Matrix3 inverse;

        /// <summary>
        ///
        /// </summary>
        /// <param name="length"></param>
        public BodyCentredCubicLattice(double length)
        {
            var b0 = new Vector3(length, 0, 0);
            var b1 = new Vector3(0, length, 0);
            var b2 = new Vector3(length / 2.0);
            basis = Matrix3.FromColumns(b0, b1, b2);
            inverse = basis.Inverse(0);
            voronoiCell = new VoronoiCell(Connectivity.BodyCentredCubic, basis);
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return basis * u;
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return inverse * v;
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisVoronoi(v, 3);
        }
    }
}
