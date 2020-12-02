using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Lattices.Transformed
{
    [Serializable]
    public class RefinedLattice : Lattice
    {
        private readonly double refinement;
        private readonly Lattice lattice;

        public RefinedLattice(Lattice lattice, int refinement)
        {
            this.Basis = lattice.Basis / refinement;
            voronoiCell = new VoronoiCell(lattice.VoronoiCell.Connections, this.Basis);
            this.refinement = refinement;
            this.lattice = lattice;
        }

        public override Vector3 ToBasis(Vector3 v)
        {
            return lattice.ToBasis(v * refinement);
        }

        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return lattice.ClosestVectorBasis(v * refinement);
        }

        public override Vector3 ToSpace(Vector3 u)
        {
            return lattice.ToSpace(u) / refinement;
        }
    }
}
