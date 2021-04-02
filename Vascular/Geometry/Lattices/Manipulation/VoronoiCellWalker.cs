using System.Collections.Generic;

namespace Vascular.Geometry.Lattices.Manipulation
{
    /// <summary>
    /// Walks the Voronoi cell of a lattice to find the closest basis.
    /// Gives more consistency than the <see cref="Lattice"/> methods at higher cost.
    /// </summary>
    public class VoronoiCellWalker
    {
        private Lattice lattice;
        private VoronoiCell voronoiCell;
        private ICollection<Vector3> visited = new List<Vector3>();

        /// <summary>
        /// How to record the vectors already visited.
        /// </summary>
        public enum RecordMode
        {
            /// <summary>
            /// 
            /// </summary>
            Set,
            /// <summary>
            /// 
            /// </summary>
            List
        }

        /// <summary>
        /// 
        /// </summary>
        public Lattice Lattice
        {
            get => lattice;
            set
            {
                lattice = value;
                voronoiCell = lattice.VoronoiCell;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public RecordMode Mode
        {
            set
            {
                visited = value switch
                {
                    RecordMode.List => new List<Vector3>(),
                    _ => new HashSet<Vector3>()
                };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lattice"></param>
        /// <param name="mode"></param>
        public VoronoiCellWalker(Lattice lattice, RecordMode mode = RecordMode.Set)
        {
            this.Lattice = lattice;
            this.Mode = mode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector3 NearestBasis(Vector3 v)
        {
            var u = lattice.ToBasis(v).NearestIntegral();
            var a = v - lattice.ToSpace(u);
            var m = voronoiCell.MostViolatedConnection(a);
            if (m != null)
            {
                do
                {
                    visited.Add(u);
                    u += m;
                    a = v - lattice.ToSpace(u);
                    m = voronoiCell.MostViolatedConnection(a);
                } while (m != null && !visited.Contains(u));
                visited.Clear();
            }
            return u;
        }
    }
}
