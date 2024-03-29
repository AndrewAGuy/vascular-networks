﻿using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// Helper methods for trees.
    /// </summary>
    public static class AxialBoundsBinaryTree
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static AxialBoundsBinaryTreeNode<T> Create<T>(IEnumerable<T> elements) where T : IAxialBoundable
        {
            return elements.Count() == 1
                ? new AxialBoundsBinaryTreeLeaf<T>(elements.First())
                : (AxialBoundsBinaryTreeNode<T>)new AxialBoundsBinaryTreeSplit<T>(elements);
        }

        /// <summary>
        /// Tests recursively, but might have stack issues.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="a"></param>
        public static void TestRecursive<U, V>(AxialBoundsBinaryTreeNode<U> u, AxialBoundsBinaryTreeNode<V> v, Action<U, V> a)
            where U : IAxialBoundable
            where V : IAxialBoundable
        {
            var ub = u.GetAxialBounds();
            var vb = v.GetAxialBounds();
            if (!ub.Intersects(vb))
            {
                return;
            }
            if (u is AxialBoundsBinaryTreeLeaf<U> lu)
            {
                if (v is AxialBoundsBinaryTreeLeaf<V> lv)
                {
                    a(lu.Element, lv.Element);
                }
                else if (v is AxialBoundsBinaryTreeSplit<V> sv)
                {
                    TestRecursive(lu, sv.Left, a);
                    TestRecursive(lu, sv.Right, a);
                }
            }
            else if (u is AxialBoundsBinaryTreeSplit<U> su)
            {
                if (v is AxialBoundsBinaryTreeLeaf<V> lv)
                {
                    TestRecursive(su.Left, lv, a);
                    TestRecursive(su.Right, lv, a);
                }
                else if (v is AxialBoundsBinaryTreeSplit<V> sv)
                {
                    TestRecursive(su.Left, sv.Left, a);
                    TestRecursive(su.Right, sv.Left, a);
                    TestRecursive(su.Left, sv.Right, a);
                    TestRecursive(su.Right, sv.Right, a);
                }
            }
        }

        private struct NodePair<U, V>
            where U : IAxialBoundable
            where V : IAxialBoundable
        {
            public NodePair(AxialBoundsBinaryTreeNode<U> u, AxialBoundsBinaryTreeNode<V> v)
            {
                this.u = u;
                this.v = v;
            }
            public AxialBoundsBinaryTreeNode<U> u;
            public AxialBoundsBinaryTreeNode<V> v;
        }

        /// <summary>
        /// Tests using a stack rather than recursion.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="a"></param>
        public static void TestFlat<U, V>(AxialBoundsBinaryTreeNode<U> u, AxialBoundsBinaryTreeNode<V> v, Action<U, V> a)
            where U : IAxialBoundable
            where V : IAxialBoundable
        {
            var s = new Stack<NodePair<U, V>>();
            s.Push(new NodePair<U, V>(u, v));
            while (s.Count > 0)
            {
                var c = s.Pop();
                var ub = c.u.GetAxialBounds();
                var vb = c.v.GetAxialBounds();
                if (!ub.Intersects(vb))
                {
                    continue;
                }
                if (c.u is AxialBoundsBinaryTreeLeaf<U> lu)
                {
                    if (c.v is AxialBoundsBinaryTreeLeaf<V> lv)
                    {
                        a(lu.Element, lv.Element);
                    }
                    else if (c.v is AxialBoundsBinaryTreeSplit<V> sv)
                    {
                        s.Push(new NodePair<U, V>(lu, sv.Left));
                        s.Push(new NodePair<U, V>(lu, sv.Right));
                    }
                }
                else if (c.u is AxialBoundsBinaryTreeSplit<U> su)
                {
                    if (c.v is AxialBoundsBinaryTreeLeaf<V> lv)
                    {
                        s.Push(new NodePair<U, V>(su.Left, lv));
                        s.Push(new NodePair<U, V>(su.Right, lv));
                    }
                    else if (c.v is AxialBoundsBinaryTreeSplit<V> sv)
                    {
                        s.Push(new NodePair<U, V>(su.Left, sv.Left));
                        s.Push(new NodePair<U, V>(su.Right, sv.Left));
                        s.Push(new NodePair<U, V>(su.Left, sv.Right));
                        s.Push(new NodePair<U, V>(su.Right, sv.Right));
                    }
                }
            }
        }

        /// <summary>
        /// Visits recursively.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="leafAction"></param>
        public static void Visit<T>(AxialBoundsBinaryTreeNode<T> node, Action<AxialBoundsBinaryTreeLeaf<T>> leafAction)
            where T : IAxialBoundable
        {
            if (node is AxialBoundsBinaryTreeLeaf<T> leaf)
            {
                leafAction(leaf);
            }
            else if (node is AxialBoundsBinaryTreeSplit<T> split)
            {
                Visit(split.Left, leafAction);
                Visit(split.Right, leafAction);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="branch"></param>
        /// <param name="action"></param>
        public static void Query<T>(this AxialBoundsBinaryTreeNode<T> node, Branch branch, Action<Branch, T> action)
            where T : IAxialBoundable
        {
            if (branch.LocalBounds.Intersects(node.GetAxialBounds()))
            {
                // We want to test this branch against everything downstream of this node.
                // We will never test this branch against anything again.
                node.Query(branch.LocalBounds, item => action(branch, item));
            }

            // If local bounds of branch didn't hit global bounds of node, then it won't hit anything downstream either.
            // Split search based on child pairs if we can, otherwise we need to keep searching down the network against this.
            if (node is AxialBoundsBinaryTreeSplit<T> split)
            {
                foreach (var child in branch.Children)
                {
                    if (child.GlobalBounds.Intersects(split.Left.GetAxialBounds()))
                    {
                        split.Left.Query(child, action);
                    }
                    if (child.GlobalBounds.Intersects(split.Right.GetAxialBounds()))
                    {
                        split.Right.Query(child, action);
                    }
                }
            }
            else
            {
                foreach (var child in branch.Children)
                {
                    if (child.GlobalBounds.Intersects(node.GetAxialBounds()))
                    {
                        node.Query(child, action);
                    }
                }
            }
        }
    }
}
