using Vascular.Geometry;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Moves a node by a relative amount or to a fixed location.
    /// </summary>
    public class PerturbNode : GeometryAction
    {
        private readonly IMobileNode n;
        private readonly Vector3 p;
        private readonly bool r;

        /// <summary>
        /// Move node <paramref name="n"/> to <paramref name="p"/> if <paramref name="r"/> (relative) is <see langword="false"/>,
        /// else move to <c>n.x + p</c>.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="p"></param>
        /// <param name="r"></param>
        public PerturbNode(IMobileNode n, Vector3 p, bool r = true)
        {
            this.n = n;
            this.p = p;
            this.r = r;
        }

        /// <inheritdoc/>
        public override void Execute(bool propagate = false)
        {
            n.Position = r ? n.Position + p : p;
            if (propagate)
            {
                n.UpdatePhysicalAndPropagate();
            }
        }
    }
}
