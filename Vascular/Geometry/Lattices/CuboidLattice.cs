using System.Runtime.Serialization;

namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// A lattice with different lengths in each axis.
    /// </summary>
    [DataContract]
    public class CuboidLattice : Lattice
    {
        [DataMember]
        private readonly double lengthX, lengthY, lengthZ;

        [DataMember]
        private readonly double inverseX, inverseY, inverseZ;

        /// <summary>
        /// 
        /// </summary>
        public enum Connection
        {
            /// <summary>
            /// 
            /// </summary>
            SquareEdges,
            /// <summary>
            /// 
            /// </summary>
            SquareEdgesVertices,
            /// <summary>
            /// 
            /// </summary>
            CubeFaces,
            /// <summary>
            /// 
            /// </summary>
            CubeFacesEdges,
            /// <summary>
            /// 
            /// </summary>
            CubeFacesEdgesVertices
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lengthX"></param>
        /// <param name="lengthZ"></param>
        /// <param name="lengthY"></param>
        /// <param name="connection"></param>
        public CuboidLattice(double lengthX, double lengthZ, double lengthY = 0.0, Connection connection = Connection.CubeFaces)
        {
            this.lengthX = lengthX;
            this.lengthY = lengthY != 0 ? lengthY : this.lengthX;
            this.lengthZ = lengthZ;
            inverseX = 1.0 / this.lengthX;
            inverseY = 1.0 / this.lengthY;
            inverseZ = 1.0 / this.lengthZ;
            this.Basis = Matrix3.Diagonal(lengthX, lengthY, lengthZ);
            Vector3[] connections = null;
            switch (connection)
            {
                case Connection.SquareEdges:
                    connections = Connectivity.SquareEdges;
                    break;
                case Connection.SquareEdgesVertices:
                    connections = Connectivity.SquareEdgesVertices;
                    break;
                case Connection.CubeFaces:
                    connections = Connectivity.CubeFaces;
                    break;
                case Connection.CubeFacesEdges:
                    connections = Connectivity.CubeFacesEdges;
                    break;
                case Connection.CubeFacesEdgesVertices:
                    connections = Connectivity.CubeFacesEdgesVertices;
                    break;
            }
            voronoiCell = new VoronoiCell(connections, this.Basis);
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return new Vector3(v.x * inverseX, v.y * inverseY, v.z * inverseZ);
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return new Vector3(u.x * lengthX, u.y * lengthY, u.z * lengthZ);
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisRounding(v);
        }
    }
}
