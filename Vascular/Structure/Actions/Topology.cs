using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions
{
    /// <summary>
    /// Helper methods for changing the network topology.
    /// </summary>
    public static class Topology
    {
        /// <summary>
        /// The existing segment becomes the parent of the new one.
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="reinit"></param>
        /// <returns></returns>
        public static Transient InsertTransient(Segment seg, bool reinit = true)
        {
            var end = seg.End;
            var tr = new Transient();
            var child = new Segment(tr, end);
            // Child relationships first, to prevent wipe of segment end.
            tr.Child = child;
            end.Parent = child;
            // Parent relationships
            seg.End = tr;
            tr.Parent = seg;
            // Update branch
            if (reinit)
            {
                seg.Branch.Reinitialize();
            }
            return tr;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Transient[] InsertTransients(Segment seg, int n)
        {
            var s = seg;
            var tr = new Transient[n];
            for (var i = 0; i < n; ++i)
            {
                tr[i] = InsertTransient(s, false);
                s = tr[i].Child;
            }
            seg.Branch.Reinitialize();
            return tr;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tran"></param>
        /// <returns></returns>
        public static Segment RemoveTransient(Transient tran)
        {
            // Simple case of rewiring existing parent to existing child end.
            var seg = tran.Parent;
            var end = tran.Child.End;
            seg.End = end;
            end.Parent = seg;
            seg.Branch.Reinitialize();
            return seg;
        }

        // /// <summary>
        // ///
        // /// </summary>
        // /// <param name="term"></param>
        // /// <param name="nullParent"></param>
        // /// <returns></returns>
        // public static Transient? CullTerminal(Terminal term, bool nullParent = true)
        // {
        //     // Was terminal ever actually built?
        //     if (term.Parent == null)
        //     {
        //         term.Culled = true;
        //         return null;
        //     }
        //     // Will this kill the whole network?
        //     var branch = term.Upstream;
        //     if (branch.Start is not Bifurcation bifurc)
        //     {
        //         throw new TopologyException("Branch to be culled does not start at bifurcation");
        //     }
        //     // Rewire sibling and parent into single branch, this turns 3 branches into 1.
        //     var parent = bifurc.Parent;
        //     var other = bifurc.Downstream[0] == branch ? bifurc.Children[1] : bifurc.Children[0];
        //     var tr = new Transient()
        //     {
        //         Position = bifurc.Position,
        //         Child = other,
        //         Parent = parent
        //     };
        //     other.Start = tr;
        //     parent.End = tr;
        //     parent.Branch.End = other.Branch.End;
        //     parent.Branch.Reinitialize();
        //     // Set as culled, cast branch into the void
        //     if (nullParent)
        //     {
        //         term.Parent = null;
        //     }
        //     term.Culled = true;
        //     return tr;
        // }



        private static BranchNode MakeBranchNode(Segment parent, Segment[] children, Network network, Vector3 position)
        {
            // Needs all segments to have valid branches attached to them.
            if (children.Length == 2)
            {
                var bf = new Bifurcation(children) { Parent = parent, Network = network, Position = position };
                parent.End = bf;
                parent.Branch.End = bf;
                return bf;
            }
            else if (children.Length > 2)
            {
                var hs = new HigherSplit(children) { Parent = parent, Network = network, Position = position };
                parent.End = hs;
                parent.Branch.End = hs;
                return hs;
            }
            throw new TopologyException();
        }

        private static INode MakeNode(Segment parent, Segment[] children, Network network, Vector3 position)
        {
            if (children.Length == 1)
            {
                var tr = new Transient()
                {
                    Parent = parent,
                    Child = children[0],
                    Position = position
                };
                parent.End = tr;
                children[0].Start = tr;
                // Update the parent branch to consume the child
                // We get the correct reference upstream once the parent segment of the end node has been updated
                var br = parent.Branch;
                br.End = children[0].Branch.End;
                br.Reinitialize();
                return tr;
            }
            else
            {
                return MakeBranchNode(parent, children, network, position);
            }
        }

        private static INode RemoveBranches(BranchNode node, ReadOnlySpan<int> idx)
        {
            var (kept, _) = node.Children.SplitArrayStack(idx);
            return MakeNode(node.Parent!, kept, node.Network, node.Position);
        }

        private static INode RemoveBranch(BranchNode node, int i)
        {
            ReadOnlySpan<int> idx = stackalloc int[1] { i };
            return RemoveBranches(node, idx);
        }

        private static (Branch, Segment) MakeBranch(BranchNode start, BranchNode end)
        {
            var s = new Segment(start, end);
            var b = new Branch(s) { Start = start, End = end };
            return (b, s);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <param name="idx"></param>
        /// <param name="onTerm"></param>
        /// <param name="onBranch"></param>
        /// <returns></returns>
        public static INode CullBranches(BranchNode node, ReadOnlySpan<int> idx,
            Action<Terminal>? onTerm, Action<Branch>? onBranch)
        {
            var (lost, kept) = node.Children.SplitArrayStack(idx);
            var repl = MakeNode(node.Parent!, kept, node.Network, node.Position);
            foreach (var culled in lost)
            {
                if (onTerm is not null)
                {
                    Terminal.ForDownstream(culled.Branch, onTerm);
                }
                if (onBranch is not null)
                {
                    onBranch(culled.Branch);
                }
            }
            return repl;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <param name="i"></param>
        /// <param name="onTerm"></param>
        /// <param name="onBranch"></param>
        /// <returns></returns>
        public static INode CullBranch(BranchNode node, int i,
            Action<Terminal>? onTerm, Action<Branch>? onBranch)
        {
            ReadOnlySpan<int> idx = stackalloc int[1] { i };
            return CullBranches(node, idx, onTerm, onBranch);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="term"></param>
        /// <param name="nullParent"></param>
        /// <returns></returns>
        /// <exception cref="TopologyException"></exception>
        public static IMobileNode? CullTerminal(Terminal term, bool nullParent = true)
        {
            term.Culled = true;
            var up = term.Parent?.Branch;
            if (up is null || up.Start is null)
            {
                return null;
            }
            if (up.Start is Source)
            {
                throw new TopologyException("Branch to be culled is root");
            }
            if (nullParent)
            {
                term.Parent = null;
            }
            var idx = up.IndexInParent;
            if (idx < 0)
            {
                return null;
            }
            return CullBranch(up.Start, idx, null, null) as IMobileNode;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="term"></param>
        /// <param name="nullParent"></param>
        /// <returns></returns>
        public static IMobileNode? CullTerminalAndPropagate(Terminal term, bool nullParent = true)
        {
            var tr = CullTerminal(term, nullParent);
            if (tr is not null)
            {
                tr.Parent!.Branch.PropagateLogicalUpstream();
                tr.UpdatePhysicalAndPropagate();
            }
            return tr;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Branch MakeFirst(Source s, Terminal t)
        {
            // Must create branch beforehand, since setting source child segment will pull branch in
            var seg = new Segment(s, t);
            var br = new Branch(seg) { Start = s, End = t };
            s.Child = seg;
            t.Parent = seg;
            t.Network = s.Network; // Forgetting this has made life so hard in the past
            return br;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Bifurcation CreateBifurcation(Segment from, BranchNode to)
        {
            // Existing segment becomes parent of bifurcation
            // Two new child segments created. Both start at new bifurcation.
            //      Child 0 ends with the existing segment end
            //      Child 1 ends with the terminal
            var end = from.End;
            var branchEnd = from.Branch.End;
            var bifurc = new Bifurcation()
            {
                Network = from.Branch.Network
            };
            // Structural relationships of children and bifurcation.
            var child0 = new Segment(bifurc, end);
            var child1 = new Segment(bifurc, to);
            bifurc.Children[0] = child0;
            bifurc.Children[1] = child1;
            end.Parent = child0;
            to.Parent = child1;
            // Structural relationship of existing segment and bifurcation.
            from.End = bifurc;
            bifurc.Parent = from;
            // Two new branches created to deal with this, one for each new child.
            // Update bifurcation downstream to pull references into bifurcation.
            var branch0 = new Branch(child0) { Start = bifurc, End = branchEnd };
            var branch1 = new Branch(child1) { Start = bifurc, End = to };
            bifurc.UpdateDownstream();
            // Update the existing branch.
            from.Branch.End = bifurc;
            from.Branch.Reinitialize();
            return bifurc;
        }

        // /// <summary>
        // ///
        // /// </summary>
        // /// <param name="bifurc"></param>
        // /// <param name="keptChild"></param>
        // /// <param name="markDownstream"></param>
        // /// <param name="nullDownstream"></param>
        // /// <param name="nullLost"></param>
        // /// <returns></returns>
        // public static Transient RemoveBifurcation(Bifurcation bifurc, int keptChild,
        //     bool markDownstream = true, bool nullDownstream = true, bool nullLost = false)
        // {
        //     var lostChild = 1 - keptChild;
        //     // Rewire bifurcation into transient, same as in culling
        //     var tr = new Transient()
        //     {
        //         Position = bifurc.Position,
        //         Child = bifurc.Children[keptChild],
        //         Parent = bifurc.Parent
        //     };
        //     tr.Parent.End = tr;
        //     tr.Child.Start = tr;
        //     tr.Parent.Branch.End = tr.Child.Branch.End;
        //     tr.Parent.Branch.Reinitialize();
        //     // Now find all downstream terminals on the lost side, remove them
        //     if (markDownstream)
        //     {
        //         Terminal.ForDownstream(bifurc.Downstream[lostChild], t =>
        //         {
        //             if (nullDownstream)
        //             {
        //                 t.Parent = null;
        //             }
        //             t.Culled = true;
        //         });
        //     }
        //     if (nullLost)
        //     {
        //         bifurc.Parent = null;
        //         bifurc.Downstream[lostChild].End.Parent = null;
        //     }
        //     return tr;
        // }

        // /// <summary>
        // ///
        // /// </summary>
        // /// <param name="branch"></param>
        // /// <param name="throwIfRoot"></param>
        // /// <param name="markDownstream"></param>
        // /// <param name="nullDownstream"></param>
        // /// <param name="nullLost"></param>
        // /// <returns></returns>
        // public static Transient? RemoveBranch(Branch branch, bool throwIfRoot = true,
        //     bool markDownstream = true, bool nullDownstream = true, bool nullLost = false)
        // {
        //     switch (branch.Start)
        //     {
        //         case Bifurcation bifurc:
        //             return RemoveBifurcation(bifurc, 1 - bifurc.IndexOf(branch), markDownstream, nullDownstream, nullLost);
        //         case Source source:
        //             if (throwIfRoot)
        //             {
        //                 throw new TopologyException("Branch to be removed is root vessel");
        //             }
        //             source.Child = null!;
        //             if (markDownstream)
        //             {
        //                 Terminal.ForDownstream(branch, t =>
        //                 {
        //                     if (nullDownstream)
        //                     {
        //                         t.Parent = null;
        //                     }
        //                     t.Culled = true;
        //                 });
        //             }
        //             return null;
        //         default:
        //             throw new TopologyException("Branch started with invalid node");
        //     }
        // }

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void SwapEnds(Branch a, Branch b)
        {
            var endA = a.End;
            var endB = b.End;
            a.End = endB;
            b.End = endA;
            a.Reset();
            b.Reset();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void SwapConnections(Segment a, Segment b)
        {
            // Update segment endings
            var endA = a.End;
            var endB = b.End;
            a.End = endB;
            b.End = endA;
            endB.Parent = a;
            endA.Parent = b;
            // Update branch endings and then branch as whole
            var branchEndA = a.Branch.End;
            var branchEndB = b.Branch.End;
            a.Branch.End = branchEndB;
            b.Branch.End = branchEndA;
            a.Branch.Reinitialize();
            b.Branch.Reinitialize();
        }

        /// <summary>
        /// Removes the branch from its parent and creates a new bifurcation from <paramref name="target"/>.
        /// </summary>
        /// <param name="moving"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static (Bifurcation, INode) MoveBifurcation(Branch moving, Segment target)
        {
            var rNode = RemoveBranch(moving.Start, moving.IndexInParent);
            var nNode = CreateBifurcation(target, moving.End);
            return (nNode, rNode);
        }

        // /// <summary>
        // ///
        // /// </summary>
        // /// <param name="moving"></param>
        // /// <param name="from"></param>
        // /// <returns></returns>
        // public static (Transient?, Bifurcation?) MoveBifurcation(Branch moving, Branch from)
        // {
        //     if (moving.Start is Bifurcation bifurc)
        //     {
        //         // Remove bifurcation, rewire parent and sibling into single branch
        //         var keptChild = 1 - bifurc.IndexOf(moving);
        //         var tr = new Transient()
        //         {
        //             Position = bifurc.Position,
        //             Child = bifurc.Children[keptChild],
        //             Parent = bifurc.Parent
        //         };
        //         tr.Parent.End = tr;
        //         tr.Child.Start = tr;
        //         tr.Parent.Branch.End = tr.Child.Branch.End;
        //         tr.Parent.Branch.Reinitialize();
        //         // Create new bifurcation, reset branches to do this
        //         moving.Reset();
        //         from.Reset();
        //         return (tr, CreateBifurcation(from.Segments[0], moving.End));
        //     }
        //     return (null, null);
        // }

        /// <summary>
        /// Given a comparer and a starting branch, canonicalize the network. This places children in order according
        /// to their 'minimal' terminal downstream. Terminals must never compare equal for this to work.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="comparer"></param>
        public static void Canonicalize(Branch from, IComparer<Terminal> comparer)
        {
            _ = Canonicalize(from.End, comparer);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bf"></param>
        public static void SwapBifurcationOrder(Bifurcation bf)
        {
            bf.Children.Swap(0, 1);
            bf.UpdateDownstream();
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static T OrderDownstream<T>(BranchNode node, Func<Branch, T> keySelector, IComparer<T> comparer)
        {
            var pairs = node.Downstream.
                Select(branch =>
                {
                    var key = keySelector(branch);
                    return (branch, key);
                })
                .OrderBy(pair => pair.key, comparer)
                .ToArray();
            for (var i = 0; i < pairs.Length; ++i)
            {
                node.Downstream[i] = pairs[i].branch;
                node.Children[i] = node.Downstream[i].Segments[0];
            }
            return pairs[0].key;
        }

        private static Terminal Canonicalize(BranchNode node, IComparer<Terminal> comparer)
        {
            if (node is Terminal terminal)
            {
                return terminal;
            }
            else if (node is Bifurcation bifurcation)
            {
                var t0 = Canonicalize(bifurcation.Downstream[0].End, comparer);
                var t1 = Canonicalize(bifurcation.Downstream[1].End, comparer);
                switch (comparer.Compare(t0, t1))
                {
                    case > 0:
                        SwapBifurcationOrder(bifurcation);
                        return t1;

                    case < 0:
                        return t0;

                    case 0:
                        throw new TopologyException("Terminals have compared equal in canonicalization, possible ambiguity");
                }
            }
            else
            {
                return OrderDownstream(node, branch => Canonicalize(branch.End, comparer), comparer);
            }
        }

        /// <summary>
        /// If optimizations have been made on clones of the original network, transfer the cloned structure to the original.
        /// This is achieved by setting the downstream section of the source node to the clone - the terminals may have changed so
        /// the new terminals are kept.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        public static void Transfer(Network to, Network from)
        {
            var node = to.Source;
            var seg = from.Source.Child;
            node.Child = seg;
            seg.Start = node;
            seg.Branch.Start = node;
        }
    }
}
