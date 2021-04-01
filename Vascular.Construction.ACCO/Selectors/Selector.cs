using Vascular.Construction.ACCO.Evaluators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Selectors
{
    /// <summary>
    /// Used by <see cref="SequentialBuilder"/> to choose where to bifurcate from for a given terminal.
    /// </summary>
    public abstract class Selector
    {
        /// <summary>
        /// The evaluation policy for a pair (<see cref="Branch"/>, <see cref="Terminal"/>).
        /// </summary>
        public IEvaluator<Branch> Evaluator { get; set; } = new MeanlineEvaluator();

        /// <summary>
        /// When given a starting <see cref="Branch"/> and target <see cref="Terminal"/>, returns the best candidate for bifurcation.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public abstract Evaluation<Branch> Select(Branch from, Terminal to);
    }
}
