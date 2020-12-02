using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vascular.Structure;

namespace Vascular.Geometry.Bounds
{
    public class AxialBoundsBinaryTree
    {
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
    }
}
