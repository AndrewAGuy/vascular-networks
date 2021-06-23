using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Functionality.Capillary;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Graphs;
using Vascular.Geometry.Lattices;
using Vascular.Geometry.Lattices.Transformed;
using Vascular.Structure;

namespace Vascular.Functionality.Liver
{
    public class LiverLattice : CapillaryBase
    {
        // Procedure: need to modify original capillary lattice
        // PV,HA,HV: join on one lattice, forbid intersection with any biliary vessels
        //      PV,HA: attachment points on HP lattice knocked-out on an index 3 sublattice
        //      HV: attachment points on the knocked out z1 = z2 mod 3 sublattice
        //      These terminal vessels are grown with a z3 mod 2 knockout to alternate planes
        //      so our capillary lattice is essentially a refinement of this mother lattice in z3
        // BT (possible double if manufacturing via 3DP), use offset lattice, forbid intersection with blood
        //      BT grows in with PT but must separate at the very final stage. Refinement of final lattice
        //      need not be as dense as the blood vessels, but must still only refine in z3 to avoid intersections.

        public Lattice CapillaryLattice { get; }
        public Lattice BiliaryLattice { get; }

        public Func<Segment, bool> IsPermittedIntersection { get; set; }

        public LiverLattice(double xy, double zCap, int bFactor = 2)
        {
            this.CapillaryLattice = new HexagonalPrismLattice(xy, zCap);
            var (b1, b2, b3) = this.CapillaryLattice.Basis.Columns;
            var offset = (b1 + b2) / 3 + b3 / 2;
            this.BiliaryLattice = new OffsetLattice(new HexagonalPrismLattice(xy, zCap * bFactor), offset);
        }

        public bool GenerateBiliary { get; set; }
        public Network[] Vasculature { get; set; }
        public Network[] BiliaryTree { get; set; }

        public override (Graph<Vertex, Edge> graph, HashSet<Vector3> boundary) GenerateChunk(AxialBounds bounds)
        {
            var (lattice, permitted, forbidden) = this.GenerateBiliary
                ? (this.BiliaryLattice, this.BiliaryTree, this.Vasculature)
                : (this.CapillaryLattice, this.Vasculature, this.BiliaryTree);
            var networks = this.Vasculature.Concat(this.BiliaryTree).ToArray();
            Func<Segment, bool> isPermitted = this.IsPermittedIntersection != null
                ? seg => this.IsPermittedIntersection(seg) && permitted.Contains(seg.Branch.Network)
                : seg => permitted.Contains(seg.Branch.Network);

            var capGen = new CapillaryLattice()
            {
                Lattice = lattice,
                Networks = networks,
                PermittedIntersection = isPermitted,
                MinOverlap = this.MinOverlap,
                Radius = this.Radius,
            };
            return capGen.GenerateChunk(bounds);
        }

        

        protected override void StitchChunks(Graph<Vertex, Edge> existing, HashSet<Vector3> existingBoundary, Graph<Vertex, Edge> adding, HashSet<Vector3> addingBoundary)
        {
            throw new NotImplementedException();
        }
    }
}
