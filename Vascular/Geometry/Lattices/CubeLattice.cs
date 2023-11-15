
namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// The most basic type of lattice.
    /// </summary>
    public class CubeLattice : Lattice
    {
        private readonly double length;
        private readonly double inverse;

        /// <summary>
        ///
        /// </summary>
        public enum Connection
        {
            /// <summary>
            ///
            /// </summary>
            Faces,
            /// <summary>
            ///
            /// </summary>
            FacesEdges,
            /// <summary>
            ///
            /// </summary>
            FacesEdgesVertices
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="length"></param>
        /// <param name="connection"></param>
        public CubeLattice(double length, Connection connection = Connection.Faces)
        {
            this.length = length;
            inverse = 1.0 / length;
            this.Basis = Matrix3.Diagonal(length);
            Vector3[] connections = connection switch
            {
                Connection.Faces => Connectivity.CubeFaces,
                Connection.FacesEdges => Connectivity.CubeFacesEdges,
                Connection.FacesEdgesVertices => Connectivity.CubeFacesEdgesVertices,
                _ => throw new PhysicalValueException()
            };
            voronoiCell = new VoronoiCell(connections, this.Basis);
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisRounding(v);
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return v * inverse;
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return u * length;
        }
    }
}
