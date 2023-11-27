using System;
using Vascular.Structure;
using Vascular.Structure.Nodes;
using Vascular.Optimization.Topological;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure.Actions;

namespace Vascular.Optimization.Geometric;

/// <summary>
/// Experimental, taking the "geometry-only" features of the old hybrid minimizer,
/// i.e., we allow "soft topology changes" such as trimming terminals that have been consumed
/// and merging branches that have collapsed.
/// This is then to be called between rounds of real topological modification explicitly performed
/// by the user, rather than trying to rank a collection of competing queued actions.
/// </summary>
public class SoftTopology
{
    /// <summary>
    /// Acts in the following order: trimming, collapsing, defragmenting.
    /// No point collapsing if pinned by a terminal, no point defragmenting vessels that are doomed.
    /// </summary>
    /// <param name="network"></param>
    public bool Update(Network network)
    {
        var acted = false;
        if (TrimTerminals(network))
        {
            this.UpdateNetwork(network);
            acted = true;
        }
        if (CollapseBranches(network))
        {
            this.UpdateNetwork(network);
            acted = true;
        }
        if (Defragment(network))
        {
            this.UpdateNetwork(network);
            acted = true;
        }
        return acted;
    }

    /// <summary>
    ///
    /// </summary>
    public Action<Network> UpdateNetwork { get; set; } = n => n.Set(true, true, true);

    /// <summary>
    ///
    /// </summary>
    public Func<Branch, bool>? Collapse { get; set; } = IsTotallyConsumed;

    /// <summary>
    ///
    /// </summary>
    public Action<Branch>? OnCollapse { get; set; }

    private bool CollapseBranches(Network network)
    {
        if (this.Collapse is null)
        {
            return false;
        }

        var collapsing = new List<Branch>(network.Branches.Count());
        foreach (var b in network.Branches)
        {
            if (b.Start is Source || b.End is Terminal)
            {
                continue;
            }
            if (this.Collapse(b))
            {
                collapsing.Add(b);
            }
        }
        foreach (var b in collapsing)
        {
            this.OnCollapse?.Invoke(b);
            Topology.Collapse(b);
        }

        return collapsing.Count != 0;
    }

    /// <summary>
    ///
    /// </summary>
    public Func<Branch, bool>? Trim { get; set; } = IsTotallyConsumed;

    /// <summary>
    ///
    /// </summary>
    public Action<Terminal>? OnTrim { get; set; }

    private bool TrimTerminals(Network network)
    {
        if (this.Trim is null)
        {
            return false;
        }

        var trim = new List<Terminal>(network.Terminals.Count());
        foreach (var t in network.Terminals)
        {
            if (this.Trim(t.Upstream))
            {
                trim.Add(t);
            }
        }
        foreach (var t in trim)
        {
            this.OnTrim?.Invoke(t);
            Topology.CullTerminal(t);
        }

        return trim.Count != 0;
    }

    /// <summary>
    ///
    /// </summary>
    public Func<Transient, bool>? DefragmentationPredicate { get; set; }

    /// <summary>
    ///
    /// </summary>
    public Func<Transient, double>? DefragmentationRadius { get; set; }

    /// <summary>
    /// More than just defragmenting - completely wipe all transient nodes. Good for performance
    /// and early stage optimizations.
    /// </summary>
    public bool RemoveTransients { get; set; }

    private bool Defragment(Network network)
    {
        var geometryInvalid = false;
        if (this.RemoveTransients)
        {
            foreach (var b in network.Branches)
            {
                if (b.Segments.Count > 1)
                {
                    b.Reset();
                    geometryInvalid = true;
                }
            }
        }
        else if (this.DefragmentationPredicate != null)
        {
            network.Source.PropagateRadiiDownstream();
            foreach (var b in network.Branches)
            {
                geometryInvalid |= Fragmentation.Defragment(b,
                    this.DefragmentationPredicate,
                    this.DefragmentationRadius ?? Fragmentation.MeanRadius);
            }
        }
        return geometryInvalid;
    }

    /// <summary>
    /// Predicate for whether a branch should be removed by whether it sticks out beyond its parent.
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool IsTotallyConsumed(Branch b)
    {
        var L2 = b.Direction.LengthSquared;
        var r2 = Math.Pow(b.Start.Parent!.Radius, 2);
        return L2 < r2;
    }
}
