using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Vascular.Geometry.Lattices
{
    [DataContract]
    public class CubeLattice : Lattice
    {
        [DataMember]
        private readonly double length;
        [DataMember]
        private readonly double inverse;

        public enum Connection
        {
            Faces,
            FacesEdges,
            FacesEdgesVertices
        }

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

        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return NearestBasisRounding(v);
        }

        public override Vector3 ToBasis(Vector3 v)
        {
            return v * inverse;
        }

        public override Vector3 ToSpace(Vector3 u)
        {
            return u * length;
        }
    }
}
