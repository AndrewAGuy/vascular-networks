namespace Vascular.Structure
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static Network Network(this Segment segment)
        {
            return segment.Branch.Network;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Network Network(this INode node)
        {
            return node.Parent.Branch.Network;
        }
    }
}
