using System;
using System.Runtime.Serialization;

namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// A lattice with a triangular pattern in plane and columns out of plane.
    /// </summary>
    public class HexagonalPrismLattice : Lattice
    {
        private readonly Matrix3 inverse;

        /// <summary>
        ///
        /// </summary>
        public enum Connection
        {
            /// <summary>
            ///
            /// </summary>
            Triangle,
            /// <summary>
            ///
            /// </summary>
            HexagonalPrismFaces,
            /// <summary>
            ///
            /// </summary>
            HexagonalPrismFacesEdges
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="length"></param>
        /// <param name="height"></param>
        /// <param name="connection"></param>
        public HexagonalPrismLattice(double length, double height, Connection connection = Connection.HexagonalPrismFaces)
        {
            var b0 = new Vector3(1.0 * length, 0, 0);
            var b1 = new Vector3(0.5 * length, Math.Sqrt(3.0) / 2.0 * length, 0);
            var b2 = new Vector3(0, 0, height);
            this.Basis = Matrix3.FromColumns(b0, b1, b2);
            inverse = this.Basis.Inverse(0);
            Vector3[] connections = connection switch
            {
                Connection.Triangle => Connectivity.Triangle,
                Connection.HexagonalPrismFaces => Connectivity.HexagonalPrismFaces,
                Connection.HexagonalPrismFacesEdges => Connectivity.HexagonalPrismFacesEdges,
                _ => throw new PhysicalValueException()
            };
            voronoiCell = new VoronoiCell(connections, this.Basis);
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisVoronoi(v, 2);
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return inverse * v;
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return basis * u;
        }
    }
}
