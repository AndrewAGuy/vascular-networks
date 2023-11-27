using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions;

/// <summary>
/// Temporary home of methods relating to higher order topology.
/// At present, we will grow new vessels by bifurcation, and higher splits will arise from merging/splitting procedures.
/// </summary>
public static class HigherTopology
{
    // TODO: Merge into main topology class.

    // /// <summary>
    // ///
    // /// </summary>
    // /// <param name="br"></param>
    // /// <returns></returns>
    // public static HigherSplit Collapse(Branch br)
    // {
    //     // We collapse the given branch, putting its children into the same grouping as its siblings.
    //     // We therefore need to swap out the end node held by the parent,
    //     // and the higher split will handle the references held by its children.
    //     var parentB = br.Parent!;
    //     var parentS = parentB.Segments[^1];

    //     var N = br.Start.Downstream.Length - 1 + br.Children.Length;
    //     var S = new Segment[N];
    //     Array.Copy(br.End.Children, S, br.Children.Length);
    //     var i = br.Children.Length;
    //     foreach (var s in br.Siblings)
    //     {
    //         S[i] = s.Segments[0];
    //         ++i;
    //     }

    //     var hs = new HigherSplit(S)
    //     {
    //         Parent = parentS,
    //         Network = br.Network,
    //         Position = br.Start.Position
    //     };
    //     parentS.End = hs;
    //     parentB.End = hs;

    //     return hs;
    // }

    // /// <summary>
    // /// Elements in <paramref name="indices"/> split into new child of <paramref name="hs"/>, which is
    // /// removed from the network and replaced with a new node.
    // /// </summary>
    // /// <param name="hs"></param>
    // /// <param name="indices"></param>
    // /// <returns></returns>
    // public static (BranchNode remaining, BranchNode leaving) SplitToChild(HigherSplit hs, int[] indices)
    // {
    //     // TODO: add in size checks here
    //     var pSeg = hs.Parent;
    //     var pBr = hs.Upstream;
    //     var (splitS, remS) = hs.Children.SplitArrayStack(indices);

    //     // Generate new child and attaching segment
    //     var sSeg = new Segment(null!, null!);
    //     BranchNode leaving = null!;
    //     if (splitS.Length == 2)
    //     {
    //         // TODO: Make this a similar function to higher split
    //         var bfSpl = new Bifurcation() { Network = hs.Network, Parent = sSeg };
    //         sSeg.End = bfSpl;
    //         bfSpl.Children[0] = splitS[0];
    //         bfSpl.Children[1] = splitS[1];
    //         splitS[0].Start = bfSpl;
    //         splitS[1].Start = bfSpl;
    //         splitS[0].Branch.Start = bfSpl;
    //         splitS[1].Branch.Start = bfSpl;
    //         bfSpl.UpdateDownstream();
    //         leaving = bfSpl;
    //     }
    //     else if (splitS.Length > 2)
    //     {
    //         var hsSpl = new HigherSplit(splitS)
    //         {
    //             Network = hs.Network,
    //             Parent = sSeg
    //         };
    //         sSeg.End = hsSpl;
    //         leaving = hsSpl;
    //     }

    //     // We have complete child endpoint, dangling segment and no branch.
    //     BranchNode remaining = null!;
    //     if (remS.Length == 1)
    //     {
    //         var bfRem = new Bifurcation() { Network = hs.Network, Parent = pSeg };
    //         pSeg.End = bfRem;
    //         pBr.End = bfRem;
    //         remaining = bfRem;
    //         bfRem.Children[0] = remS[0];
    //         remS[0].Start = bfRem;
    //         remS[0].Branch.Start = bfRem;
    //         bfRem.Children[1] = sSeg;
    //         sSeg.Start = bfRem;
    //         var brSeg = new Branch(sSeg) { End = leaving, Start = bfRem };
    //         bfRem.UpdateDownstream();
    //     }
    //     else if (remS.Length > 1)
    //     {
    //         var hsRem = new HigherSplit() { Network = hs.Network, Parent = pSeg };
    //         pSeg.End = hsRem;
    //         pBr.End = hsRem;
    //         remaining = hsRem;
    //         var remSC = new List<Segment>(remS).Append(sSeg).ToArray();
    //         var brSeg = new Branch(sSeg) { End = leaving, Start = hsRem };
    //         hsRem.Initialize(remSC);
    //     }

    //     return (remaining, leaving);
    // }

    // /// <summary>
    // /// Elements in <paramref name="indices"/> split into a sibling of <paramref name="hs"/>, which is
    // /// replaced with a new node.
    // /// </summary>
    // /// <param name="hs"></param>
    // /// <param name="indices"></param>
    // /// <returns></returns>
    // public static (BranchNode remaining, BranchNode leaving) SplitToSibling(HigherSplit hs, int[] indices)
    // {
    //     var pSeg = hs.Parent;
    //     var pBr = hs.Upstream;
    //     var (splitS, remS) = hs.Children.SplitArrayStack(indices);
    //     var pBf = new Bifurcation() { Parent = pSeg, Network = hs.Network };
    //     pSeg.End = pBf;
    //     pBr.End = pBf;

    //     var sRem = new Segment(pBf, null!);
    //     var sSpl = new Segment(pBf, null!);
    //     var brRem = new Branch(sRem) { Start = pBf };
    //     var brSpl = new Branch(sSpl) { Start = pBf };
    //     pBf.Children[0] = sRem;
    //     pBf.Children[1] = sSpl;
    //     pBf.UpdateDownstream();
    //     pBf.UpdateChildTopology();

    //     var nRem = MakeBranchNode(sRem, remS, hs.Network, Vector3.INVALID);
    //     var nSpl = MakeBranchNode(sSpl, splitS, hs.Network, Vector3.INVALID);

    //     return (nRem, nSpl);
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
            parent.Branch.Reinitialize();
            return tr;
        }
        else
        {
            return MakeBranchNode(parent, children, network, position);
        }
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
    /// <param name="hs"></param>
    /// <param name="idx"></param>
    /// <param name="markDownstream"></param>
    /// <param name="nullDownstream"></param>
    /// <param name="nullLost"></param>
    /// <returns></returns>
    public static INode Remove(HigherSplit hs, int[] idx, bool markDownstream = true, bool nullDownstream = true, bool nullLost = false)
    {
        var (kept, lost) = hs.Children.SplitArrayStack(idx);
        var newNode = MakeBranchNode(hs.Parent, kept, hs.Network, Vector3.INVALID);
        foreach (var L in lost)
        {
            if (markDownstream)
            {
                Terminal.ForDownstream(L.Branch, t =>
                {
                    if (nullDownstream)
                    {
                        t.Parent = null;
                    }
                    t.Culled = true;
                });
            }
            if (nullLost)
            {
                L.Start = null!;
                L.Branch.Start = null!;
                L.Branch.End.Parent = null!;
                L.Branch.End = null!;
            }
        }
        if (nullLost)
        {
            hs.Parent = null;
        }
        return newNode;
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
        var (kept, lost) = node.Children.SplitArrayStack(idx);
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
    /// <param name="onTerm"></param>
    /// <param name="onBranch"></param>
    /// <returns></returns>
    /// <exception cref="TopologyException"></exception>
    public static INode? CullTerminal(Terminal term,
        Action<Terminal>? onTerm, Action<Branch>? onBranch)
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
        term.Parent = null;
        var idx = up.IndexInParent;
        if (idx < 0)
        {
            return null;
        }
        return CullBranch(up.Start, idx, onTerm, onBranch);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="child"></param>
    /// <returns></returns>
    public static INode AddChild(INode node, BranchNode child)
    {
        var seg = new Segment(node, child);
        _ = new Branch(seg) { End = child };
        return AddChild(node, seg);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    /// <param name="child"></param>
    /// <returns></returns>
    public static INode AddChild(INode node, Branch child)
    {
        return AddChild(node, child.Segments[0]);
    }

    /// <summary>
    /// Replaces <paramref name="node"/> to also have <paramref name="child"/> as a child.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="child"></param>
    /// <returns></returns>
    /// <exception cref="TopologyException"></exception>
    public static INode AddChild(INode node, Segment child)
    {
        var p = node.Parent ?? throw new TopologyException("Cannot add child to source node");
        var c = new Segment[node.Children.Length + 1];
        Array.Copy(node.Children, c, node.Children.Length);
        c[^1] = child;
        return MakeBranchNode(p, c, node.Network(), node.Position);
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
        var nNode = Topology.CreateBifurcation(target, moving.End);
        return (nNode, rNode);
    }

    /// <summary>
    /// If only one branch is requested to split, leaves the network unchanged and returns <paramref name="hs"/>
    /// and the child specified by <paramref name="indices"/>. Otherwise, creates a new node with the requested
    /// children grouped together, and makes a new intermediate branch to bridge between the remaining children and
    /// the split children. Replaces <paramref name="hs"/> in the network with a node containing the remaining
    /// children and the intermediate branch.
    /// </summary>
    /// <param name="hs"></param>
    /// <param name="indices"></param>
    /// <returns></returns>
    public static (BranchNode remaining, BranchNode leaving) SplitToChild(HigherSplit hs, ReadOnlySpan<int> indices)
    {
        if (indices.Length == 1)
        {
            return (hs, hs.Downstream[indices[0]].End);
        }

        var (splS, remS) = hs.Children.SplitArrayStack(indices);
        var sInt = MakeBlank();

        var nSpl = MakeBranchNode(sInt, splS, hs.Network, Vector3.INVALID);

        var remC = new Segment[remS.Length + 1];
        Array.Copy(remS, remC, remS.Length);
        remC[^1] = sInt;
        var nRem = MakeBranchNode(hs.Parent, remC, hs.Network, Vector3.INVALID);

        return (nRem, nSpl);
    }

    /// <summary>
    /// Creates a new <see cref="Bifurcation"/>, whose children are the children of <paramref name="hs"/>
    /// split by <paramref name="indices"/>. If a child set has more than one branch, an intermediate
    /// branch and splitting node is created to group them together, otherwise the existing branch and end node
    /// is returned.
    /// </summary>
    /// <param name="hs"></param>
    /// <param name="indices"></param>
    /// <returns></returns>
    public static (BranchNode remaining, BranchNode leaving, Bifurcation parent)
        SplitToSibling(HigherSplit hs, ReadOnlySpan<int> indices)
    {
        var (splS, remS) = hs.Children.SplitArrayStack(indices);
        var bfC = MakeBlank(2);
        BranchNode nRem, nSpl;

        if (remS.Length > 1)
        {
            nRem = MakeBranchNode(bfC[0], remS, hs.Network, Vector3.INVALID);
        }
        else
        {
            nRem = remS[0].Branch.End;
            bfC[0] = remS[0];
        }

        if (splS.Length > 1)
        {
            nSpl = MakeBranchNode(bfC[1], splS, hs.Network, Vector3.INVALID);
        }
        else
        {
            nSpl = splS[0].Branch.End;
            bfC[1] = splS[0];
        }

        var nBf = (Bifurcation)MakeBranchNode(hs.Parent, bfC, hs.Network, Vector3.INVALID);

        return (nRem, nSpl, nBf);
    }

    private static Segment MakeBlank()
    {
        var s = new Segment(null!, null!);
        _ = new Branch(s);
        return s;
    }

    private static Segment[] MakeBlank(int N)
    {
        var S = new Segment[N];
        for (var i = 0; i < N; ++i)
        {
            S[i] = MakeBlank();
        }
        return S;
    }
}
