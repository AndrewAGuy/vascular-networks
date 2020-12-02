﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Lattices.Transformed
{
    [Serializable]
    public class OffsetLattice : Lattice
    {
        private readonly Lattice lattice;
        private readonly Vector3 offset;

        public OffsetLattice(Lattice lattice, Vector3 offset)
        {
            this.lattice = lattice;
            this.offset = offset;
            voronoiCell = lattice.VoronoiCell;
            this.Basis = lattice.Basis;
        }

        public override Vector3 ClosestVectorBasis(Vector3 v)
        {
            return lattice.ClosestVectorBasis(v - offset);
        }

        public override Vector3 ToBasis(Vector3 v)
        {
            return lattice.ToBasis(v - offset);
        }

        public override Vector3 ToSpace(Vector3 u)
        {
            return lattice.ToSpace(u) + offset;
        }
    }
}
