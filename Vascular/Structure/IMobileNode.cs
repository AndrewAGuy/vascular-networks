namespace Vascular.Structure
{
    /// <summary>
    /// A node that can be moved and easily update only what is needed.
    /// </summary>
    public interface IMobileNode : INode
    {
        /// <summary>
        /// Call after moving to update the derived values.
        /// </summary>
        void UpdatePhysicalAndPropagate();
    }
}
