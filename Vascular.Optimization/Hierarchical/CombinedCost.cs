using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;

namespace Vascular.Optimization.Hierarchical
{
    public class CombinedCost : HierarchicalCost
    {
        public HierarchicalCost[] Costs { get; set; }
        public Func<HierarchicalCost[], double> Combiner { get; set; }
        public Func<HierarchicalCost[], double[]> Gradient { get; set; }

        private double[] gradients;

        public override double Cost => this.Combiner(this.Costs);

        public override Vector3 PositionGradient(IMobileNode node)
        {
            var gradient = new Vector3();
            for (var i = 0; i < gradients.Length; ++i)
            {
                gradient += this.Costs[i].PositionGradient(node) * gradients[i];
            }
            return gradient;
        }

        public override double FlowGradient(Branch branch)
        {
            var gradient = 0.0;
            for (var i = 0; i < gradients.Length; ++i)
            {
                gradient += this.Costs[i].FlowGradient(branch) * gradients[i];
            }
            return gradient;
        }

        public override double ReducedResistanceGradient(Branch branch)
        {
            var gradient = 0.0;
            for (var i = 0; i < gradients.Length; ++i)
            {
                gradient += this.Costs[i].ReducedResistanceGradient(branch) * gradients[i];
            }
            return gradient;
        }

        public override void SetCache()
        {
            foreach (var cost in this.Costs)
            {
                cost.SetCache();
            }
            gradients = this.Gradient(this.Costs);
        }
    }
}
