namespace Vascular.Geometry.Lattices.Transformed
{
    /// <summary>
    /// Arbitrary affine transform.
    /// </summary>
    public class RotatedOffsetLattice : Lattice
    {
        private readonly Lattice lattice;
        private readonly Matrix3 rotation;
        private readonly Matrix3 inverse;
        private readonly Vector3 offset;

        /// <summary>
        ///
        /// </summary>
        /// <param name="lattice"></param>
        /// <param name="rotation"></param>
        /// <param name="offset"></param>
        public RotatedOffsetLattice(Lattice lattice, Matrix3 rotation, Vector3 offset)
        {
            this.lattice = lattice;
            this.rotation = rotation;
            inverse = rotation.Inverse();
            this.offset = offset;
            this.Basis = rotation * lattice.Basis;
            voronoiCell = new VoronoiCell(lattice.VoronoiCell.Connections, this.Basis);
        }

        /// <inheritdoc/>
        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return lattice.ClosestVectorBasis(inverse * (v - offset));
        }

        /// <inheritdoc/>
        public override Vector3 ToBasis(Vector3 v)
        {
            return lattice.ToBasis(inverse * (v - offset));
        }

        /// <inheritdoc/>
        public override Vector3 ToSpace(Vector3 u)
        {
            return rotation * lattice.ToSpace(u) + offset;
        }
    }
}
