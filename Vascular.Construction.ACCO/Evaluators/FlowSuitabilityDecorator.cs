using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Evaluators
{
    /// <summary>
    /// Prevents bifurcations from high-flow branches.
    /// </summary>
    public class FlowSuitabilityDecorator : IEvaluator<Branch>
    {
        /// <summary>
        /// 
        /// </summary>
        public IEvaluator<Branch> InnerEvaluator { get; set; } = new MeanlineEvaluator();

        /// <summary>
        /// The maximum flow.
        /// </summary>
        public double MaximumFlow { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="t"></param>
        /// <returns></returns>
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
