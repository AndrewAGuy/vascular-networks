using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Evaluators
{
    /// <summary>
    /// Evaluates a candidate object with respect to a terminal.
    /// </summary>
    /// <typeparam name="T">Kept for historical reasons.</typeparam>
    public interface IEvaluator<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Evaluation<T> Evaluate(T o, Terminal t);
    }
}
