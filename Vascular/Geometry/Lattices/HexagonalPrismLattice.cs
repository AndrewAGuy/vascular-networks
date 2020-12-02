using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Lattices
{
    [Serializable]
    public class HexagonalPrismLattice : Lattice
    {
        private readonly Matrix3 inverse;

        public enum Connection
        {
            Triangle,
            HexagonalPrismFaces,
            HexagonalPrismFacesEdges
        }

        public HexagonalPrismLattice(double length, double height, Connection connection = Connection.HexagonalPrismFaces)
        {
            var b0 = new Vector3(1.0 * length, 0, 0);
            var b1 = new Vector3(0.5 * length, Math.Sqrt(3.0) / 2.0 * length, 0);
            var b2 = new Vector3(0, 0, height);
            this.Basis = Matrix3.FromColumns(b0, b1, b2);
            inverse = this.Basis.Inverse();
            Vector3[] connections = null;
            switch (connection)
            {
                case Connection.Triangle:
                    connections = Connectivity.Triangle;
                    break;
                case Connection.HexagonalPrismFaces:
                    connections = Connectivity.HexagonalPrismFaces;
                    break;
                case Connection.HexagonalPrismFacesEdges:
                    connections = Connectivity.HexagonalPrismFacesEdges;
                    break;
            }
            voronoiCell = new VoronoiCell(connections, this.Basis);
        }

        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisVoronoi(v, 2);
        }

        public override Vector3 ToBasis(Vector3 v)
        {
            return inverse * v;
        }

        public override Vector3 ToSpace(Vector3 u)
        {
            return basis * u;
        }
    }
}
