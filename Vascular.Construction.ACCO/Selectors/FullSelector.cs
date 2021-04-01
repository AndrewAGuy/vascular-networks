using Vascular.Construction.ACCO.Evaluators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Selectors
{
    /// <summary>
    /// Checks every possible branch for the worst case performance.
    /// </summary>
    public class FullSelector : Selector
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

        private Evaluation<Branch> Select(Evaluation<Branch> parent, Terminal to)
        {
            parent.Object.End.SetChildRadii();
            var best = parent;
            foreach (var child in parent.Object.Children)
            {
                var childEval = Select(this.Evaluator.Evaluate(child, to), to);
                if (childEval.Suitable && childEval.Cost < best.Cost)
                {
                    best = childEval;
                }
            }
            return best;
        }
    }
}
