namespace Vascular.Structure.Actions
{
    /// <summary>
    /// An action that moves nodes without affecting the topology.
    /// </summary>
    public abstract class GeometryAction
    {
        /// <summary>
        /// Makes the move, then optionally propagates the derived values upstream.
        /// </summary>
        /// <param name="propagate"></param>
        public abstract void Execute(bool propagate = false);
    }
}
