using Vascular.Construction.ACCO.Evaluators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Selectors
{
    /// <summary>
    /// A more relaxed version of <see cref="LazySelector"/>. 
    /// Moves downstream until <see cref="Limit"/> number of sequential failures to improve.
    /// </summary>
    public class CountedSelector : Selector
    {
        /// <summary>
        /// The numner of sequential failures before termination.
        /// </summary>
        public int Limit { get; set; } = 2;

        /// <summary>
        /// Compare to the best seen upstream rather than the parent. 
        /// If false, performance may be reduced but a more optimal candidate may be found.
        /// </summary>
        public bool CompareBest { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public override Evaluation<Branch> Select(Branch from, Terminal to)
        {
            var eval = this.Evaluator.Evaluate(from, to);
            return this.CompareBest
                ? SelectBest(eval, eval, to, 0)
                : SelectChild(eval, to, 0);
        }

        private Evaluation<Branch> SelectChild(Evaluation<Branch> selection, Terminal node, int failures)
        {
            var children = selection.Object.Children;
            selection.Object.End.SetChildRadii();
            var best = selection;
            foreach (var child in children)
            {
                var childSelection = this.Evaluator.Evaluate(child, node);
                if (childSelection.Cost < selection.Cost)
                {
                    var downstream = SelectChild(childSelection, node, 0);
                    if (downstream.Suitable && downstream.Cost < best.Cost)
                    {
                        best = downstream;
                    }
                }
                else
                {
                    if (failures < this.Limit)
                    {
                        var downstream = SelectChild(childSelection, node, failures + 1);
                        if (downstream.Suitable && downstream.Cost < best.Cost)
                        {
                            best = downstream;
                        }
                    }
                }
            }
            return best;
        }

        private Evaluation<Branch> SelectBest(Evaluation<Branch> selection, Evaluation<Branch> parent, Terminal node, int failures)
        {
            var children = parent.Object.Children;
            parent.Object.End.SetChildRadii();
            var best = selection;
            foreach (var child in children)
            {
                var childSelection = this.Evaluator.Evaluate(child, node);
                if (childSelection.Cost < selection.Cost)
                {
                    var downstream = SelectBest(childSelection, childSelection, node, 0);
                    if (downstream.Suitable && downstream.Cost < best.Cost)
                    {
                        best = downstream;
                    }
                }
                else
                {
                    if (failures < this.Limit)
                    {
                        // Only compare to best seen so far! Can't risk eliminating by comparing to entire other tree
                        var downstream = SelectBest(selection, childSelection, node, failures + 1);
                        if (downstream.Suitable && downstream.Cost < best.Cost)
                        {
                            best = downstream;
                        }
                    }
                }
            }
            return best;
        }
    }
}
