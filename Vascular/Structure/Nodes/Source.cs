using System;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// The source node of a tree, required for setting radii.
    /// </summary>
    public abstract class Source : BranchNode
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        public Source(Vector3 x)
        {
            SetPosition(x);
        }

        private Vector3 position = Vector3.INVALID;

        private Segment child = null!;
        private Branch down = null!;
        private readonly Segment[] children = new Segment[1];
        private readonly Branch[] downstream = new Branch[1];

        /// <summary>
        /// Updates <see cref="Children"/> and <see cref="Downstream"/> when set.
        /// </summary>
        public Segment Child
        {
            get => child;
            set
            {
                child = value;
                children[0] = value;
                if (value != null)
                {
                    down = value.Branch;
                    downstream[0] = value.Branch;
                }
                else
                {
                    down = null!;
                    downstream[0] = null!;
                }
            }
        }

        /// <inheritdoc/>
        sealed public override Segment[] Children => children;

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        public void SetPosition(Vector3 x)
        {
            position = x ?? throw new PhysicalValueException("Source position must not be null");
        }

        /// <inheritdoc/>
        sealed public override double Flow => child!.Flow;

        /// <inheritdoc/>
        sealed public override Branch[] Downstream => downstream;

        /// <inheritdoc/>
        sealed public override Segment? Parent
        {
            get => null;
            set => throw new TopologyException("Source node has no parent");
        }

        /// <inheritdoc/>
        sealed public override Branch? Upstream => null;

        /// <inheritdoc/>
        sealed public override Vector3 Position
        {
            get => position;
            set => throw new GeometryException("Source node position is fixed");
        }

#if !NoEffectiveLength
        /// <inheritdoc/>
        sealed public override double EffectiveLength => down!.EffectiveLength;

        /// <summary>
        /// See DOI 10.1109/TBME.2019.2942313 for how this works.
        /// </summary>
        public abstract double Volume { get; }
#endif

        /// <inheritdoc/>
        public sealed override double ReducedResistance => down!.ReducedResistance;

        /// <summary>
        /// The total network resistance.
        /// </summary>
        public abstract double Resistance { get; }

        /// <summary>
        /// The fluid mechanical work to move <see cref="Flow"/> through <see cref="Resistance"/>.
        /// </summary>
        public abstract double Work { get; }

        /// <inheritdoc/>
        sealed public override void PropagateLogicalUpstream()
        {
            return;
        }

        /// <inheritdoc/>
        sealed public override void PropagatePhysicalUpstream()
        {
            return;
        }

        /// <summary>
        /// The root radius. May be specified or derived depending on choice of source.
        /// </summary>
        public abstract double RootRadius { get; }

        /// <inheritdoc/>
        sealed public override void SetChildRadii()
        {
            down!.Radius = this.RootRadius;
        }

        /// <inheritdoc/>
        sealed public override void PropagateRadiiDownstream()
        {
            SetChildRadii();
            down!.UpdateRadii();
            down.End.PropagateRadiiDownstream();
        }

        /// <inheritdoc/>
        sealed public override void PropagateRadiiDownstream(double pad)
        {
            SetChildRadii();
            down!.End.PropagateRadiiDownstream(pad);
            down.Radius += pad;
            down.UpdateRadii();
        }

        /// <inheritdoc/>
        public sealed override void PropagateRadiiDownstream(Func<Branch, double> postProcessing)
        {
            SetChildRadii();
            down!.End.PropagateRadiiDownstream(postProcessing);
            down.Radius = postProcessing(down);
            down.UpdateRadii();
        }

#if !NoDepthPathLength
        /// <inheritdoc/>
        sealed public override void CalculatePathLengthsAndDepths()
        {
            depth = 0;
            down!.End.CalculatePathLengthsAndDepths();
        }

        /// <inheritdoc/>
        public sealed override void CalculatePathLengthsAndOrder()
        {
            down!.End.CalculatePathLengthsAndOrder();
            depth = down.Depth;
        }

        /// <summary>
        /// Always 0.
        /// </summary>
        sealed public override double PathLength => 0.0;

        /// <summary>
        /// Always 0 when depth, but non zero for Strahler order.
        /// </summary>
        sealed public override int Depth => depth;

        private int depth = 0;
#endif

        /// <inheritdoc/>
        sealed public override void CalculatePhysical()
        {
            down!.UpdateLengths();
            down.UpdatePhysicalLocal();
            down.End.CalculatePhysical();
            down.UpdatePhysicalGlobal();
        }

#if !NoPressure
        /// <inheritdoc/>
        sealed public override void CalculatePressures()
        {
            down!.End.CalculatePressures();
        }
#endif

        /// <inheritdoc/>
        sealed public override AxialBounds GenerateDownstreamBounds()
        {
            return down!.GenerateDownstreamBounds();
        }

        /// <inheritdoc/>
        sealed public override AxialBounds GenerateDownstreamBounds(double pad)
        {
            return down!.GenerateDownstreamBounds(pad);
        }

        /// <summary>
        /// Sets the root radius to match <paramref name="target"/>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="current"></param>
        public abstract void SetTargetRadius(double target, double current);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public abstract Source Clone();
    }
}
