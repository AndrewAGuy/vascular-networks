﻿using System;

namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// A tetrahedral lattice.
    /// </summary>
    public class TetrahedronLattice : Lattice
    {
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
            inverse = this.Basis.Inverse(0);
            Vector3[] connections = connection switch
            {
                Connection.Triangle => Connectivity.Triangle,
                Connection.Tetrahedron => Connectivity.Tetrahedron,
                _ => throw new PhysicalValueException()
            };
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
