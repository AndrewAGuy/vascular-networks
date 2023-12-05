using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization.Hierarchical;

internal class MultiNetworkCost : HierarchicalCost
{
    public Dictionary<Network, HierarchicalCost> Costs { get; set; } = new();

    public override double Cost => this.Costs.Values.Sum(c => c.Cost);

    public override double FlowGradient(Branch branch)
    {
        return this.Costs[branch.Network].FlowGradient(branch);
    }

    public override Vector3 PositionGradient(IMobileNode node)
    {
        return this.Costs[node.Network()].PositionGradient(node);
    }

    public override double ReducedResistanceGradient(Branch branch)
    {
        return this.Costs[branch.Network].ReducedResistanceGradient(branch);
    }

    public override void SetCache(Network? network = null)
    {
        foreach (var (n, c) in this.Costs)
        {
            c.SetCache(n);
        }
    }

    public override double SetCost(Network? network = null)
    {
        foreach (var (n, c) in this.Costs)
        {
            c.SetCost(n);
        }
        return this.Cost;
    }
}
