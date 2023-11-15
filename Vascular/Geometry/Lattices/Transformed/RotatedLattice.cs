namespace Vascular.Geometry.Lattices.Transformed
{
    /// <summary>
    /// Lattice with rotation. Does not verify that the rotation is a rotation.
    /// </summary>
    public class RotatedLattice : Lattice
    {
        private readonly Lattice lattice;
        private readonly Matrix3 rotation;
        private readonly Matrix3 inverse;

        /// <summary>
        ///
        /// </summary>
        /// <param name="lattice"></param>
        /// <param name="rotation"></param>
        public RotatedLattice(Lattice lattice, Matrix3 rotation)
        {
            this.lattice = lattice;
            this.rotation = rotation;
            inverse = rotation.Inverse();
            this.Basis = rotation * lattice.Basis;
            voronoiCell = new VoronoiCell(lattice.VoronoiCell.Connections, this.Basis);
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return lattice.ClosestVectorBasis(inverse * v);
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return lattice.ToBasis(inverse * v);
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return rotation * lattice.ToSpace(u);
        }
    }
}
