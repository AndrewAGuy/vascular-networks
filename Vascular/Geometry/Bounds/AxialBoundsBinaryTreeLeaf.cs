using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Bounds
{
    public class AxialBoundsBinaryTreeLeaf<T> : AxialBoundsBinaryTreeNode<T> where T : IAxialBoundable
    {
        public T Element { get; }

        public AxialBoundsBinaryTreeLeaf(T e) : base(e.GetAxialBounds())
        {
            this.Element = e;
        }

        public override void Query(AxialBounds query, Action<T> action)
        {
            if (bounds.Intersects(query))
            {
                action(this.Element);
            }
        }

        public override void UpdateBounds()
        {
            bounds = this.Element.GetAxialBounds();
        }
    }
}
