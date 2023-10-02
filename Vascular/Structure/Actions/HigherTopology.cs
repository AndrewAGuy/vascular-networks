using System;
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

        var hs = new HigherSplit();
        hs.Initialize(S);
        hs.Parent = parentS;
        parentS.End = hs;
        parentB.End = hs;

        return hs;
    }

    /// <summary>
    /// Elements in <paramref name="indices"/> split into new child of <paramref name="hs"/>, which is
    /// adapted to its new size.
    /// </summary>
    /// <param name="hs"></param>
    /// <param name="indices"></param>
    /// <returns></returns>
    public static (BranchNode remaining, BranchNode leaving) SplitToChild(HigherSplit hs, int[] indices)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Elements in <paramref name="indices"/> split into a sibling of <paramref name="hs"/>, which is
    /// adapted to its new size.
    /// </summary>
    /// <param name="hs"></param>
    /// <param name="indices"></param>
    /// <returns></returns>
    public static (BranchNode remaining, BranchNode leaving) SplitToSibling(HigherSplit hs, int[] indices)
    {
        throw new NotImplementedException();
    }

    // TODO: Implement methods for:
    //  - Removing a branch / multiple branches (e.g. culling terminals)
    //  - Splitting into multiple clusters
    // Requring methods for:
    //  - Initializing/adding/removing branches from a higher split.
}