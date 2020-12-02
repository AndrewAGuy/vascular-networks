using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Lattices.Manipulation
{
    public class VoronoiCellWalker
    {
        private Lattice lattice;
        private VoronoiCell voronoiCell;
        private ICollection<Vector3> visited = new List<Vector3>();

        public enum RecordMode
        {
            Set,
            List
        }

        public Lattice Lattice
        {
            get
            {
                return lattice;
            }
            set
            {
                lattice = value;
                voronoiCell = lattice.VoronoiCell;
            }
        }

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

        public VoronoiCellWalker(Lattice lattice, RecordMode mode = RecordMode.Set)
        {
            this.Lattice = lattice;
            this.Mode = mode;
        }

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
