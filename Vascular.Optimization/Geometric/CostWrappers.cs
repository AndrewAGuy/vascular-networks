using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Optimization.Hierarchical;
using Vascular.Structure;

namespace Vascular.Optimization.Geometric;

/// <summary>
///
/// </summary>
public class HierarchicalCostWrapper : IGradientDescentCost
{
    private readonly HierarchicalCost cost;

    /// <summary>
    ///
    /// </summary>
    /// <param name="cost"></param>
    public HierarchicalCostWrapper(HierarchicalCost cost)
    {
        this.cost = cost;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public double Cost(Network n)
    {
        return cost.SetCost(n);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public (double, IEnumerable<GradientEntry>) CostGradient(Network n, Func<IMobileNode, bool>? p)
    {
        cost.SetCache(n);
        if (p is not null)
        {
            IEnumerable<GradientEntry> gradients()
            {
                foreach (var node in n.MobileNodes)
                {
                    if (p(node))
                    {
                        yield return new(node, cost.PositionGradient(node));
                    }
                }
            }
            return (cost.Cost, gradients());
        }
        else
        {
            IEnumerable<GradientEntry> gradients()
            {
                foreach (var node in n.MobileNodes)
                {
                    yield return new(node, cost.PositionGradient(node));
                }
            }
            return (cost.Cost, gradients());
        }
    }
}

/// <summary>
///
/// </summary>
public class SmootherWrapper : IGradientDescentCost
{
    private readonly Smoother smoother;

    /// <summary>
    ///
    /// </summary>
    /// <param name="smoother"></param>
    public SmootherWrapper(Smoother smoother)
    {
        this.smoother = smoother;
        smoother.Scaling = -1;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public double Cost(Network n)
    {
        smoother.Forces(n, out var e);
        return e;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="n"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public (double, IEnumerable<GradientEntry>) CostGradient(Network n, Func<IMobileNode, bool>? p)
    {
        smoother.RecordPredicate = p ?? (n => true);
        var F = smoother.Forces(n, out var e);
        return (e, F.Select(p => new GradientEntry(p.Key, p.Value)));
    }
}
