using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Hierarchical;

internal class MultiSourceCost : HierarchicalCost
{
    public Dictionary<Source, HierarchicalCost> Costs { get; set; } = new();

    public override double Cost => this.Costs.Values.Sum(c => c.Cost);

    public override double FlowGradient(Branch branch)
    {
        return this.Costs[branch.Origin].FlowGradient(branch);
    }

    public override Vector3 PositionGradient(IMobileNode node)
    {
        return this.Costs[node.Origin()].PositionGradient(node);
    }

    public override double ReducedResistanceGradient(Branch branch)
    {
        return this.Costs[branch.Origin].ReducedResistanceGradient(branch);
    }

    public override void SetCache(Source? source = null)
    {
        foreach (var (n, c) in this.Costs)
        {
            c.SetCache(n);
        }
    }

    public override double SetCost(Source? source = null)
    {
        foreach (var (n, c) in this.Costs)
        {
            c.SetCost(n);
        }
        return this.Cost;
    }
}
