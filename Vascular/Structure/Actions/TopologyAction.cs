namespace Vascular.Structure.Actions
{
    /// <summary>
    /// An action that may be executed to change the network topology.
    /// </summary>
    public abstract class TopologyAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propagateLogical"></param>
        /// <param name="propagatePhysical"></param>
        public abstract void Execute(bool propagateLogical = true, bool propagatePhysical = false);

        /// <summary>
        /// Whether the action may be taken without creating a non-tree structure.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsPermissable();
    }
}
