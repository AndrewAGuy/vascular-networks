using System.Runtime.Serialization;

namespace Vascular.Geometry.Lattices.Transformed
{
    /// <summary>
    /// A lattice whose basis has been scaled, useful for subdivision.
    /// Uses integral scaling, so returns a superlattice.
    /// </summary>
    [DataContract]
    public class RefinedLattice : Lattice
    {
        [DataMember]
        private readonly double refinement;
        [DataMember]
        private readonly Lattice lattice;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lattice"></param>
        /// <param name="refinement"></param>
        public RefinedLattice(Lattice lattice, int refinement)
        {
            this.Basis = lattice.Basis / refinement;
            voronoiCell = new VoronoiCell(lattice.VoronoiCell.Connections, this.Basis);
            this.refinement = refinement;
            this.lattice = lattice;
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return lattice.ToBasis(v * refinement);
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return lattice.ClosestVectorBasis(v * refinement);
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return lattice.ToSpace(u) / refinement;
        }
    }
}
