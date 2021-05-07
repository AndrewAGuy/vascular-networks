using System;
using System.Collections;
using System.Collections.Generic;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// Base type for creating a binary splitting tree for bounds queries.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AxialBoundsBinaryTreeNode<T> : IAxialBoundsQueryable<T>, IAxialBoundable where T : IAxialBoundable
    {
        /// <summary>
        /// 
        /// </summary>
        protected AxialBounds bounds;

        /// <summary>
        /// Number of leaf nodes downstream. Used for profiling.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public AxialBoundsBinaryTreeNode(AxialBounds b, int c)
        {
            bounds = b;
            this.Count = c;
        }

        /// <inheritdoc/>
        public AxialBounds GetAxialBounds()
        {
            return bounds;
        }

        /// <inheritdoc/>
        public abstract void Query(AxialBounds query, Action<T> action);

        /// <summary>
        /// Recalculate the bounds if changes have been made.
        /// </summary>
        public abstract void UpdateBounds();

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            var T = new List<T>();
            AxialBoundsBinaryTree.Visit(this, t => T.Add(t.Element));
            return T.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
