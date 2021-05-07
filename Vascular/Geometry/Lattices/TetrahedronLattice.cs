using System;
using System.Runtime.Serialization;

namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// A tetrahedral lattice.
    /// </summary>
    [DataContract]
    public class TetrahedronLattice : Lattice
    {
        [DataMember]
        private readonly Matrix3 inverse;

        /// <summary>
        /// 
        /// </summary>
        public enum Connection
        {
            /// <summary>
            /// Triangles in plane.
            /// </summary>
            Triangle,
            /// <summary>
            /// Full 3D.
            /// </summary>
            Tetrahedron
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        /// <param name="connection"></param>
        public TetrahedronLattice(double length, Connection connection = Connection.Tetrahedron)
        {
            var b0 = new Vector3(1.0 * length, 0, 0);
            var b1 = new Vector3(0.5 * length, Math.Sqrt(3.0) / 2.0 * length, 0);
            var b2 = new Vector3(0.5 * length, Math.Sqrt(3.0) / 6.0 * length, Math.Sqrt(2.0) / Math.Sqrt(3.0) * length);
            this.Basis = new Matrix3(
                b0.x, b1.x, b2.x,
                b0.y, b1.y, b2.y,
                b0.z, b1.z, b2.z);
            inverse = this.Basis.Inverse();
            Vector3[] connections = null;
            switch (connection)
            {
                case Connection.Triangle:
                    connections = Connectivity.Triangle;
                    break;
                case Connection.Tetrahedron:
                    connections = Connectivity.Tetrahedron;
                    break;
            }
            voronoiCell = new VoronoiCell(connections, this.Basis);
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return inverse * v;
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisVoronoi(v, 2);
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return basis * u;
        }
    }
}
