using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Construction.ACCO.Evaluators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Selectors
{
    public class LazySelector : Selector
    {
        public override Evaluation<Branch> Select(Branch from, Terminal to)
        {
            return Select(this.Evaluator.Evaluate(from, to), to);
        }

        private Evaluation<Branch> Select(Evaluation<Branch> current, Terminal to)
        {
            current.Object.End.SetChildRadii();
            var best = current;
            foreach (var child in current.Object.Children)
            {
                var childEval = this.Evaluator.Evaluate(child, to);
                if (childEval.Cost < best.Cost)
                {
                    best = childEval;
                }
            }
            if (best != current)
            {
                best = Select(best, to);
            }
            return best.Suitable ? best : current;
        }
    }
}
