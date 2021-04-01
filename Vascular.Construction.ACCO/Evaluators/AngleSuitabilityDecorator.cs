using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Evaluators
{
    /// <summary>
    /// Adds a test for angle to prevent bifurcations where the branches don't separate.
    /// </summary>
    public class AngleSuitabilityDecorator : IEvaluator<Branch>
    {
        /// <summary>
        /// 
        /// </summary>
        public IEvaluator<Branch> InnerEvaluator { get; set; } = new MeanlineEvaluator();

        /// <summary>
        /// The cosine of the minimum angle.
        /// </summary>
        public double MaximumCosine { get; set; } = 1.0;

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
