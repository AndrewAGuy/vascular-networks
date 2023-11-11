using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Vascular.Geometry;

namespace Vascular.Structure.Nodes
{
    /// <summary>
    /// Links segments in a branch.
    /// </summary>
    public class Transient : IMobileNode
    {
        /// <inheritdoc/>
        [NotNull]
        public Segment? Parent { get; set; } = null;

        private Segment child = null!;

        /// <summary>
        /// Access to the child branch. Updates <see cref="Children"/> when set.
        /// </summary>
        public Segment Child
        {
            get => child;
            set
            {
                child = value;
                this.Children[0] = value;
            }
        }

        /// <inheritdoc/>
        public Segment[] Children { get; } = new Segment[1];

        /// <inheritdoc/>
        public Vector3 Position { get; set; } = Vector3.INVALID;

        /// <inheritdoc/>
        public void UpdatePhysicalAndPropagate()
        {
            this.Parent.UpdateLength();
            child.UpdateLength();
            child.Branch.UpdatePhysicalLocal();
            child.Branch.PropagatePhysicalUpstream();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double MinInnerLength()
        {
            return Math.Sqrt(Math.Min(
                Vector3.DistanceSquared(this.Position, this.Parent.Start.Position),
                Vector3.DistanceSquared(this.Position, this.Child.End.Position)
                ));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double MaxInnerLength()
        {
            return Math.Sqrt(Math.Max(
                Vector3.DistanceSquared(this.Position, this.Parent.Start.Position),
                Vector3.DistanceSquared(this.Position, this.Child.End.Position)
                ));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double OuterLength()
        {
            return Vector3.Distance(this.Parent.Start.Position, this.Child.End.Position);
        }
    }
}
