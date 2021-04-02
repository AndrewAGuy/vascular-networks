using System.Runtime.Serialization;

namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// The most basic type of lattice.
    /// </summary>
    [DataContract]
    public class CubeLattice : Lattice
    {
        [DataMember]
        private readonly double length;
        [DataMember]
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
            Vector3[] connections = null;
            switch (connection)
            {
                case Connection.Faces:
                    connections = Connectivity.CubeFaces;
                    break;
                case Connection.FacesEdges:
                    connections = Connectivity.CubeFacesEdges;
                    break;
                case Connection.FacesEdgesVertices:
                    connections = Connectivity.CubeFacesEdgesVertices;
                    break;
            }
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
