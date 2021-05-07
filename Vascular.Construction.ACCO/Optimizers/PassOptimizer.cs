using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Optimizers
{
    /// <summary>
    /// Does nothing. Use in conjunction with batch optimization.
    /// </summary>
    public class PassOptimizer : IBifurcationOptimizer
    {
        /// <summary>
        /// "I've heard that hard work never killed anyone, but I say why take the chance?" ― Ronald Reagan
        /// </summary>
        /// <param name="b"></param>
        public void Optimize(Bifurcation b)
        {
            return;
        }
    }
}
