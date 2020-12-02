using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Lattices
{
    [Serializable]
    public class CuboidLattice : Lattice
    {
        private readonly double lengthX, lengthY, lengthZ;
        private readonly double inverseX, inverseY, inverseZ;

        public enum Connection
        {
            SquareEdges,
            SquareEdgesVertices,
            CubeFaces,
            CubeFacesEdges,
            CubeFacesEdgesVertices
        }

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

        public override Vector3 ToBasis(Vector3 v)
        {
            return new Vector3(v.x * inverseX, v.y * inverseY, v.z * inverseZ);
        }

        public override Vector3 ToSpace(Vector3 u)
        {
            return new Vector3(u.x * lengthX, u.y * lengthY, u.z * lengthZ);
        }

        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisRounding(v);
        }
    }
}
