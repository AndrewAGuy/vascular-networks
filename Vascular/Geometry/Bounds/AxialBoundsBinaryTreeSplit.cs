﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// A splitting node with 2 children.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AxialBoundsBinaryTreeSplit<T> : AxialBoundsBinaryTreeNode<T> where T : IAxialBoundable
    {
        /// <summary>
        ///
        /// </summary>
        public AxialBoundsBinaryTreeNode<T> Left { get; }

        /// <summary>
        ///
        /// </summary>
        public AxialBoundsBinaryTreeNode<T> Right { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="elements"></param>
        public AxialBoundsBinaryTreeSplit(IEnumerable<T> elements) : base(elements.GetTotalBounds(), elements.Count())
        {
            if (this.Count == 2)
            {
                this.Left = new AxialBoundsBinaryTreeLeaf<T>(elements.First());
                this.Right = new AxialBoundsBinaryTreeLeaf<T>(elements.Last());
                return;
            }

            var below = new List<T>(this.Count);
            var above = new List<T>(this.Count);
            // Split along longest edge
            var range = bounds.Range;
            var half = (bounds.Lower + bounds.Upper) / 2.0;
            if (range.x > range.y)
            {
                if (range.x > range.z)
                {
                    Split(elements, half.x, v => v.x, below, above);
                }
                else
                {
                    Split(elements, half.z, v => v.z, below, above);
                }
            }
            else if (range.y > range.z)
            {
                Split(elements, half.y, v => v.y, below, above);
            }
            else
            {
                Split(elements, half.z, v => v.z, below, above);
            }

            this.Left = AxialBoundsBinaryTree.Create(below);
            this.Right = AxialBoundsBinaryTree.Create(above);
        }

        /// <inheritdoc/>
        public override void Query(AxialBounds query, Action<T> action)
        {
            if (bounds.Intersects(query))
            {
                this.Left.Query(query, action);
                this.Right.Query(query, action);
            }
        }

        /// <inheritdoc/>
        public override bool Query(AxialBounds query, Func<T, bool> action)
        {
            if (bounds.Intersects(query))
            {
                // This short circuits, so if LHS returns true we terminate up the chain
                return this.Left.Query(query, action)
                    || this.Right.Query(query, action);
            }
            return false;
        }

        /// <inheritdoc/>
        public override void UpdateBounds()
        {
            this.Left.UpdateBounds();
            this.Right.UpdateBounds();
            bounds = this.Left.GetAxialBounds() + this.Right.GetAxialBounds();
        }

        private static void Split(IEnumerable<T> elements, double split, Func<Vector3, double> value, List<T> below, List<T> above)
        {
            var delayed = new List<T>(elements.Count());
            foreach (var element in elements)
            {
                var bounds = element.GetAxialBounds();
                var lower = value(bounds.Lower);
                var upper = value(bounds.Upper);
                // Is it completely on one side?
                if (upper < split)
                {
                    below.Add(element);
                }
                else if (lower > split)
                {
                    above.Add(element);
                }
                else
                {
                    var lowerDifference = split - lower;
                    var upperDifference = upper - split;
                    if (lowerDifference < upperDifference)
                    {
                        above.Add(element);
                    }
                    else if (upperDifference < lowerDifference)
                    {
                        below.Add(element);
                    }
                    else
                    {
                        delayed.Add(element);
                    }
                }
            }
            foreach (var element in delayed)
            {
                if (below.Count < above.Count)
                {
                    below.Add(element);
                }
                else
                {
                    above.Add(element);
                }
            }
            // Ensure that we have at least one element in each collection. If this happens, we are likely getting machine precision effects.
            if (below.Count == 0)
            {
                var start = above.Count / 2;
                for (var i = start; i < above.Count; ++i)
                {
                    below.Add(above[i]);
                }
                above.RemoveRange(start, below.Count);
            }
            else if (above.Count == 0)
            {
                var start = below.Count / 2;
                for (var i = start; i < below.Count; ++i)
                {
                    above.Add(below[i]);
                }
                below.RemoveRange(start, above.Count);
            }
        }
    }
}
