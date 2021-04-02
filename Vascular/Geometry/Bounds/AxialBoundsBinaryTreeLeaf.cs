using System;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// The data end of a splitting tree.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AxialBoundsBinaryTreeLeaf<T> : AxialBoundsBinaryTreeNode<T> where T : IAxialBoundable
    {
        /// <summary>
        /// 
        /// </summary>
        public T Element { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public AxialBoundsBinaryTreeLeaf(T e) : base(e.GetAxialBounds(), 1)
        {
            this.Element = e;
        }

        /// <inheritdoc/>
        public override void Query(AxialBounds query, Action<T> action)
        {
            if (bounds.Intersects(query))
            {
                action(this.Element);
            }
        }

        /// <inheritdoc/>
        public override void UpdateBounds()
        {
            bounds = this.Element.GetAxialBounds();
        }
    }
}
