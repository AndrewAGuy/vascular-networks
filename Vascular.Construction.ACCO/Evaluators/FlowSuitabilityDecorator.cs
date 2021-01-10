using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Evaluators
{
    public class FlowSuitabilityDecorator : IEvaluator<Branch>
    {
        public IEvaluator<Branch> InnerEvaluator { get; set; } = new MeanlineEvaluator();
        public double MaximumFlow { get; set; }

        public Evaluation<Branch> Evaluate(Branch o, Terminal t)
        {
            var eval = this.InnerEvaluator.Evaluate(o, t);
            if (eval.Cost >= 0.0)
            {
                eval.Suitable = eval.Suitable && o.Flow <= this.MaximumFlow;
            }
            return eval;
        }
    }
}
