﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vascular.Geometry.Bounds
{
    public abstract class AxialBoundsBinaryTreeNode<T> : IAxialBoundsQueryable<T>, IAxialBoundable where T : IAxialBoundable
    {
        protected AxialBounds bounds;
        public int Count { get; }

        public AxialBoundsBinaryTreeNode(AxialBounds b, int c)
        {
            bounds = b;
            this.Count = c;
        }

        public AxialBounds GetAxialBounds()
        {
            return bounds;
        }

        public abstract void Query(AxialBounds query, Action<T> action);

        public abstract void UpdateBounds();

        public static AxialBoundsBinaryTreeNode<T> Create(IEnumerable<T> elements)
        {
            return elements.Count() == 1
                ? new AxialBoundsBinaryTreeLeaf<T>(elements.First())
                : (AxialBoundsBinaryTreeNode<T>)new AxialBoundsBinaryTreeSplit<T>(elements);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var T = new List<T>();
            AxialBoundsBinaryTree.Visit(this, t => T.Add(t.Element));
            return T.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
