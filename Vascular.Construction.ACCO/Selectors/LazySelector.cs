using Vascular.Construction.ACCO.Evaluators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Selectors
{
    /// <summary>
    /// The most aggressively work-shy selection policy. Moves downstream until the children are no longer better.
    /// </summary>
    public class LazySelector : Selector
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
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
