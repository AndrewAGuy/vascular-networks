using System;
using System.Linq;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static double Flow(this INode node)
        {
            return node.Parent.Flow;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static double MaxRadius(this INode node)
        {
            return (node.Parent, node.Children.Length) switch
            {
                (Segment p, 0) => p.Radius,
                (Segment p, _) => Math.Max(p.Radius, node.Children.Max(c => c.Radius)),
                (null, 0) => double.NaN,
                (null, _) => node.Children.Max(c => c.Radius)
            };
        }
    }
}
