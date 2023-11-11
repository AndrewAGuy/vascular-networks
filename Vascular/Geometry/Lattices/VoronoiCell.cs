using System.Runtime.Serialization;

namespace Vascular.Geometry.Lattices
{
    /// <summary>
    /// A collection of half spaces that define the Voronoi cell of a lattice.
    /// </summary>
    public class VoronoiCell
    {
        private struct HalfSpace
        {
            public double x, y, z, d;
            public double Distance(Vector3 v)
            {
                return v.x * x + v.y * y + v.z * z;
            }
        }
        private readonly HalfSpace[] halfSpaces;

        /// <summary>
        ///
        /// </summary>
        /// <param name="basisConnections"></param>
        /// <param name="basis"></param>
        public VoronoiCell(Vector3[] basisConnections, Matrix3 basis)
        {
            this.Connections = basisConnections;
            halfSpaces = new HalfSpace[basisConnections.Length];
            for (var i = 0; i < basisConnections.Length; ++i)
            {
                var v = basis * basisConnections[i];
                var mv = v.Length;
                halfSpaces[i].x = v.x / mv;
                halfSpaces[i].y = v.y / mv;
                halfSpaces[i].z = v.z / mv;
                halfSpaces[i].d = mv * 0.5;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public Vector3[] Connections { get; }

        /// <summary>
        /// The connection vector associated with the greatest half-space violation.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector3 MostViolatedConnection(Vector3 v)
        {
            Vector3 mostViolated = null!;
            var greatestViolation = 0.0;
            for (var i = 0; i < halfSpaces.Length; ++i)
            {
                var distance = halfSpaces[i].Distance(v);
                if (distance > halfSpaces[i].d)
                {
                    var violation = distance - halfSpaces[i].d;
                    if (violation > greatestViolation)
                    {
                        greatestViolation = violation;
                        mostViolated = this.Connections[i];
                    }
                }
            }
            return mostViolated;
        }
    }
}
