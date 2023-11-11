using System;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes.Pinned
{
    /// <summary>
    /// Represents a <see cref="Terminal"/> which may move, but only within a certain radius.
    /// Designed for tricky thin-shell domains and tight fits for collision resolution.
    /// </summary>
    public class MobileTerminal : Terminal, IMobileNode
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="Q"></param>
        /// <param name="r"></param>
        public MobileTerminal(Vector3 x, double Q, double r) : base(x, Q)
        {
            actualPosition = x;
            this.PinningRadius = r;
        }

        private double pinningRadius;

        /// <summary>
        /// The radius around the base <see cref="Terminal.Position"/> that the actual position may be.
        /// </summary>
        public double PinningRadius
        {
            get => pinningRadius;
            set => pinningRadius = Math.Max(value, 0);
        }

        private Vector3 actualPosition;

        /// <summary>
        /// Returns the actual position: the pinning position is stored in the base <see cref="Terminal.Position"/>.
        /// </summary>
        public override Vector3 Position
        {
            get => actualPosition;
            set => actualPosition = value.ClampToBall(position, pinningRadius);
        }

        /// <summary>
        /// Updates the parent segment, branch and propagates.
        /// </summary>
        public void UpdatePhysicalAndPropagate()
        {
            this.Parent.UpdateLength();
            this.Upstream.UpdatePhysicalLocal();
            this.Upstream.PropagatePhysicalUpstream();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override Terminal Clone()
        {
            return new MobileTerminal(position.Copy(), flow, pinningRadius);
        }
    }
}
