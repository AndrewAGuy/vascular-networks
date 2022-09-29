using Vascular.Geometry;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Whilst a wrapper for <see cref="Topology.InsertTransient(Segment, bool)"/>, this doesn't have an impact on
    /// the branch-based topology.
    /// </summary>
    public class InsertTransient : GeometryAction
    {
        private readonly Segment s;
        private readonly Vector3 p;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="p"></param>
        public InsertTransient(Segment s, Vector3 p)
        {
            this.s = s;
            this.p = p;
        }

        /// <inheritdoc/>
        public override void Execute(bool propagate = false)
        {
            var t = Topology.InsertTransient(s);
            t.Position = p;
            if (propagate)
            {
                t.UpdatePhysicalAndPropagate();
            }
        }
    }
}
