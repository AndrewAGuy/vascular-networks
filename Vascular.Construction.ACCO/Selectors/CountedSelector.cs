using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Construction.ACCO.Evaluators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Selectors
{
    public class CountedSelector : Selector
    {
        public int Limit { get; set; } = 2;

        public bool CompareBest { get; set; } = false;

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
