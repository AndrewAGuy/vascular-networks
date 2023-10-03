using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Actions;

/// <summary>
/// Temporary home of methods relating to higher order topology.
/// At present, we will grow new vessels by bifurcation, and higher splits will arise from merging/splitting procedures.
/// </summary>
public static class HigherTopology
{
    // TODO: Merge into main topology class.

    /// <summary>
    /// 
    /// </summary>
    /// <param name="br"></param>
    /// <returns></returns>
    public static HigherSplit Collapse(Branch br)
    {
        // We collapse the given branch, putting its children into the same grouping as its siblings.
        // We therefore need to swap out the end node held by the parent, 
        // and the higher split will handle the references held by its children.
        var parentB = br.Parent;
        var parentS = parentB.Segments[^1];

        var N = br.Start.Downstream.Length - 1 + br.Children.Length;
        var S = new Segment[N];
        Array.Copy(br.End.Children, S, br.Children.Length);
        var i = br.Children.Length;
        foreach (var s in br.Siblings)
        {
            S[i] = s.Segments[0];
            ++i;
        }

        var hs = new HigherSplit(S)
        {
            Parent = parentS,
            Network = br.Network
        };
        parentS.End = hs;
        parentB.End = hs;

        return hs;
    }

    /// <summary>
    /// Elements in <paramref name="indices"/> split into new child of <paramref name="hs"/>, which is
    /// removed from the network and replaced with a new node.
    /// </summary>
    /// <param name="hs"></param>
    /// <param name="indices"></param>
    /// <returns></returns>
    public static (BranchNode remaining, BranchNode leaving) SplitToChild(HigherSplit hs, int[] indices)
    {
        // TODO: add in size checks here
        var pSeg = hs.Parent;
        var pBr = hs.Upstream;
        var (splitS, remS) = hs.Children.SplitArrayStack(indices);

        // Generate new child and attaching segment
        var sSeg = new Segment();
        BranchNode leaving = null;
        if (splitS.Length == 2)
        {
            // TODO: Make this a similar function to higher split
            var bfSpl = new Bifurcation() { Network = hs.Network, Parent = sSeg };
            sSeg.End = bfSpl;
            bfSpl.Children[0] = splitS[0];
            bfSpl.Children[1] = splitS[1];
            splitS[0].Start = bfSpl;
            splitS[1].Start = bfSpl;
            splitS[0].Branch.Start = bfSpl;
            splitS[1].Branch.Start = bfSpl;
            bfSpl.UpdateDownstream();
            leaving = bfSpl;
        }
        else if (splitS.Length > 2)
        {
            var hsSpl = new HigherSplit(splitS)
            {
                Network = hs.Network,
                Parent = sSeg
            };
            sSeg.End = hsSpl;
            leaving = hsSpl;
        }

        // We have complete child endpoint, dangling segment and no branch.
        BranchNode remaining = null;
        if (remS.Length == 1)
        {
            var bfRem = new Bifurcation() { Network = hs.Network, Parent = pSeg };
            pSeg.End = bfRem;
            pBr.End = bfRem;
            remaining = bfRem;
            bfRem.Children[0] = remS[0];
            remS[0].Start = bfRem;
            remS[0].Branch.Start = bfRem;
            bfRem.Children[1] = sSeg;
            sSeg.Start = bfRem;
            var brSeg = new Branch(sSeg) { End = leaving, Start = bfRem };
            bfRem.UpdateDownstream();
        }
        else if (remS.Length > 1)
        {
            var hsRem = new HigherSplit() { Network = hs.Network, Parent = pSeg };
            pSeg.End = hsRem;
            pBr.End = hsRem;
            remaining = hsRem;
            var remSC = new List<Segment>(remS).Append(sSeg).ToArray();
            var brSeg = new Branch(sSeg) { End = leaving, Start = hsRem };
            hsRem.Initialize(remSC);
        }

        return (remaining, leaving);
    }

    /// <summary>
    /// Elements in <paramref name="indices"/> split into a sibling of <paramref name="hs"/>, which is
    /// replaced with a new node.
    /// </summary>
    /// <param name="hs"></param>
    /// <param name="indices"></param>
    /// <returns></returns>
    public static (BranchNode remaining, BranchNode leaving) SplitToSibling(HigherSplit hs, int[] indices)
    {
        var pSeg = hs.Parent;
        var pBr = hs.Upstream;
        var (splitS, remS) = hs.Children.SplitArrayStack(indices);
        var pBf = new Bifurcation() { Parent = pSeg, Network = hs.Network };
        pSeg.End = pBf;
        pBr.End = pBf;

        var sRem = new Segment() { Start = pBf };
        var sSpl = new Segment() { Start = pBf };
        var brRem = new Branch(sRem) { Start = pBf };
        var brSpl = new Branch(sSpl) { Start = pBf };
        pBf.Children[0] = sRem;
        pBf.Children[1] = sSpl;
        pBf.UpdateDownstream();
        pBf.UpdateChildTopology();

        var nRem = MakeNode(sRem, remS, hs.Network);
        var nSpl = MakeNode(sSpl, splitS, hs.Network);

        return (nRem, nSpl);
    }

    /// <summary>
    /// Needs all segments to have valid branches attached to them.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="children"></param>
    /// <param name="network"></param>
    /// <returns></returns>
    /// <exception cref="TopologyException"></exception>
    public static BranchNode MakeNode(Segment parent, Segment[] children, Network network)
    {
        if (children.Length == 2)
        {
            var bf = new Bifurcation() { Parent = parent, Network = network };
            parent.End = bf;
            parent.Branch.End = bf;
            bf.SetChildren(children);
            bf.UpdateDownstream();
            bf.UpdateChildTopology();
            return bf;
        }
        else if (children.Length > 2)
        {
            var hs = new HigherSplit(children) { Parent = parent, Network = network };
            parent.End = hs;
            parent.Branch.End = hs;
            return hs;
        }
        throw new TopologyException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static (Branch, Segment) MakeBranch(BranchNode start = null, BranchNode end = null)
    {
        var s = new Segment() { Start = start, End = end };
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
        var newNode = MakeNode(hs.Parent, kept, hs.Network);
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
                L.Start = null;
                L.Branch.Start = null;
                L.Branch.End.Parent = null;
                L.Branch.End = null;
            }
        }
        if (nullLost)
        {
            hs.Parent = null;
        }
        return newNode;
    }

    // TODO: Implement methods for:
    //  - Removing a branch / multiple branches (e.g. culling terminals)
}