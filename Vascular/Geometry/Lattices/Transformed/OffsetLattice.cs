namespace Vascular.Geometry.Lattices.Transformed
{
    /// <summary>
    /// Not technically a lattice, but useful and behaves similarly.
    /// </summary>
    public class OffsetLattice : Lattice
    {
        private readonly Lattice lattice;
        private readonly Vector3 offset;

        /// <summary>
        ///
        /// </summary>
        /// <param name="lattice"></param>
        /// <param name="offset"></param>
        public OffsetLattice(Lattice lattice, Vector3 offset)
        {
            this.lattice = lattice;
            this.offset = offset;
            voronoiCell = lattice.VoronoiCell;
            this.Basis = lattice.Basis;
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return lattice.ClosestVectorBasis(v - offset);
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return lattice.ToBasis(v - offset);
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return lattice.ToSpace(u) + offset;
        }
    }
}
