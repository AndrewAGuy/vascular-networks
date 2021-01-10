using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Evaluators
{
    public class AngleSuitabilityDecorator : IEvaluator<Branch>
    {
        public IEvaluator<Branch> InnerEvaluator { get; set; } = new MeanlineEvaluator();
        public double MaximumCosine { get; set; } = 1.0;

        public Evaluation<Branch> Evaluate(Branch o, Terminal t)
        {
            var eval = this.InnerEvaluator.Evaluate(o, t);
            if (eval.Cost >= 0.0)
            {
                var dirA = t.Position - o.Start.Position;
                var dirB = o.Direction;
                var dot = dirA.Normalize() * dirB.Normalize();
                if (dot > this.MaximumCosine)
                {
                    eval.Suitable = false;
                }
            }
            return eval;
        }
    }
}
