using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Optimization.Geometric;
using Vascular.Optimization.Hierarchical;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Diagnostics;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Topological;

/// <summary>
///
/// </summary>
public static class Splitting
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="root"></param>
    /// <param name="cost"></param>
    /// <param name="generateAll"></param>
    /// <param name="onSplit"></param>
    /// <returns></returns>
    public static int MakeSplits(Branch root, HierarchicalCost cost, int generateAll = 5,
        Action<HigherSplit, int[]>? onSplit = null)
    {
        var splits = GenerateSplits(root, cost, generateAll).ToList();
        foreach (var (hs, idx) in splits)
        {
            onSplit?.Invoke(hs, idx);
            var (rem, spl) = Topology.SplitToChild(hs, idx);
            spl.Upstream!.UpdateLogical();
            Unfolder.AverageParentAndChild(rem, spl, b => b.Flow);
        }
        return splits.Count;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="root"></param>
    /// <param name="cost"></param>
    /// <param name="generateAll"></param>
    /// <returns></returns>
    public static IEnumerable<(HigherSplit hs, int[] s)> GenerateSplits(Branch root, HierarchicalCost cost,
        int generateAll = 5)
    {
        var be = new BranchEnumerator();
        foreach (var node in be.Nodes(root))
        {
            if (node is HigherSplit hs)
            {
                if (hs.Downstream.Length <= generateAll)
                {
                    if (FullSplitToChild(hs, cost) is int[] split)
                    {
                        yield return (hs, split);
                    }
                }
                else
                {
                    if (GreedySplitToChild(hs, cost) is int[] split)
                    {
                        yield return (hs, split);
                    }
                }
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="hs"></param>
    /// <param name="cost"></param>
    /// <returns></returns>
    public static int[]? GreedySplitToChild(HigherSplit hs, HierarchicalCost cost)
    {
        var (_, fc) = Forces(cost, hs);

        var (sBest, fBest) = GetBestPair(hs, cost, fc);

        var sCurrent = sBest;
        while (sCurrent!.Length < hs.Downstream.Length - 2)
        {
            (sCurrent, var fCurrent) = GetBestAddition(hs, cost, fc, sCurrent!);
            if (fCurrent > fBest)
            {
                sBest = sCurrent;
                fBest = fCurrent;
            }
        }

        return fBest <= 0 ? null : sBest;
    }

    private static (int[]?, double) GetBestAddition(HigherSplit hs, HierarchicalCost cost, Vector3[] fc, int[] G)
    {
        int[]? sBest = null;
        var fBest = double.NegativeInfinity;
        foreach (var split in GenerateAllAdditions(hs.Downstream.Length, G))
        {
            var f = SplittingForce(cost, hs, fc, split.Span);
            if (f > fBest)
            {
                sBest = split.ToArray();
                fBest = f;
            }
        }
        return (sBest, fBest);
    }

    private static (int[]?, double) GetBestPair(HigherSplit hs, HierarchicalCost cost, Vector3[] fc)
    {
        int[]? sBest = null;
        var fBest = double.NegativeInfinity;
        foreach (var split in GenerateAllPairs(hs.Downstream.Length))
        {
            var f = SplittingForce(cost, hs, fc, split.Span);
            if (f > fBest)
            {
                sBest = split.ToArray();
                fBest = f;
            }
        }
        return (sBest, fBest);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="hs"></param>
    /// <param name="cost"></param>
    /// <returns></returns>
    public static int[]? FullSplitToChild(HigherSplit hs, HierarchicalCost cost)
    {
        int[]? sBest = null;
        var fBest = double.NegativeInfinity;
        var (_, fc) = Forces(cost, hs);

        foreach (var split in GenerateAllSplitsToChild(hs.Downstream.Length))
        {
            var f = SplittingForce(cost, hs, fc, split.Span);
            if (f > fBest)
            {
                sBest = split.ToArray();
                fBest = f;
            }
        }

        return fBest <= 0 ? null : sBest;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="N"></param>
    /// <param name="G"></param>
    /// <returns></returns>
    public static IEnumerable<ReadOnlyMemory<int>> GenerateAllAdditions(int N, int[] G)
    {
        var A = new int[G.Length + 1];
        Array.Copy(G, A, G.Length);
        var h = new bool[N];
        for (var i = 0; i < G.Length; ++i)
        {
            h[G[i]] = true;
        }

        for (var i = 0; i < N; ++i)
        {
            if (!h[i])
            {
                A[^1] = i;
                yield return A;
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="N"></param>
    /// <returns></returns>
    public static IEnumerable<ReadOnlyMemory<int>> GenerateAllPairs(int N)
    {
        var split = new int[2];
        for (var i = 0; i < N; ++i)
        {
            for (var j = i + 1; j < N; ++j)
            {
                split[0] = i;
                split[1] = j;
                yield return split;
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="N"></param>
    /// <returns></returns>
    public static IEnumerable<ReadOnlyMemory<int>> GenerateAllSplitsToChild(int N)
    {
        var masks = new uint[N];
        for (var i = 0; i < N; ++i)
        {
            masks[i] = (uint)1 << i;
        }

        var split = new int[N];
        for (uint j = 0; j < (uint)Math.Pow(2, N) - 1; ++j)
        {
            var popcnt = System.Numerics.BitOperations.PopCount(j);
            if (popcnt < 2)
            {
                continue;
            }
            var idx = 0;
            for (var i = 0; i < N; ++i)
            {
                if ((j & masks[i]) != 0)
                {
                    split[idx] = i;
                    ++idx;
                }
            }
            yield return new ReadOnlyMemory<int>(split, 0, idx);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="cost"></param>
    /// <param name="hs"></param>
    /// <param name="cf"></param>
    /// <param name="G"></param>
    /// <returns></returns>
    public static double SplittingForce(HierarchicalCost cost, HigherSplit hs, Vector3[] cf, ReadOnlySpan<int> G)
    {
        var F = new Vector3();
        foreach (var g in G)
        {
            F += cf[g];
        }
        var fc = F.Length;
        var fg = IntermediateForce(cost, hs, G);
        // fg = dC/dLG, so < 0 implies pushing, > 0 pulling
        // fc defined as net pulling force away from node
        return fc - fg;
    }

    /// <summary>
    /// Define spring force as being -dC/dL, where L directly feeds into R*
    /// </summary>
    /// <param name="cost"></param>
    /// <param name="hs"></param>
    /// <returns></returns>
    public static (Vector3, Vector3[]) Forces(HierarchicalCost cost, HigherSplit hs)
    {
        // If dC/dL < 0, we have force pulling to shorten branch and vice versa
        // So define with vector towards hs: x_hs - x, times dC/dL

        var f = cost.ReducedResistanceGradient(hs.Upstream);
        var Fp = f * hs.Parent.Direction.Normalize();

        var Fc = new Vector3[hs.Children.Length];
        for (var i = 0; i < hs.Children.Length; ++i)
        {
            f = cost.ReducedResistanceGradient(hs.Downstream[i]);
            Fc[i] = -f * hs.Children[i].Direction.Normalize();
        }

        return (Fp, Fc);
    }

    /// <summary>
    /// Suppose that the branches in <paramref name="G"/> were split to a child of <paramref name="hs"/>.
    /// What is the force experienced by that branch?
    /// </summary>
    /// <param name="cost"></param>
    /// <param name="hs"></param>
    /// <param name="G"></param>
    /// <returns></returns>
    public static double IntermediateForce(HierarchicalCost cost, HigherSplit hs, ReadOnlySpan<int> G)
    {
        // Requires knowledge of splitting rule for a new branch
        // Group G together, have kG = Rg*Qg/fg^4, and RG = LG + (sum fg^4/Rg)^-1
        // For LG=0, have RG = kG/QG
        // Keep ratios fg constant: take a step such dkG = const: dRg = fg^4/Qg * dR, then dkG = dR
        // Then we have dC/dR = sum dC/dRg * fg^4/Qg = dC/dRG * dRG/dR
        // dRG/dR = 1/QG, we then have worked out the equivalent stride to emulate cost change from LG by passing to children
        var QG = 0.0;
        Span<double> R = stackalloc double[G.Length];
        Span<double> Q = stackalloc double[G.Length];
        for (var i = 0; i < G.Length; ++i)
        {
            var g = G[i];
            var d = hs.Downstream[g];
            QG += d.Flow;
            R[i] = d.ReducedResistance;
            Q[i] = d.Flow;
        }
        Span<double> pf = stackalloc double[G.Length];
        hs.Network.Splitting.Fractions(R, Q, pf);
        var dCG = 0.0;
        for (var i = 0; i < G.Length; ++i)
        {
            dCG += cost.ReducedResistanceGradient(hs.Downstream[G[i]]) * Math.Pow(pf[i], 4) / Q[i];
        }
        return dCG * QG;
    }
}
