using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Optimizers
{
    /// <summary>
    /// Called after initial placement of <see cref="Bifurcation.Position"/>.
    /// </summary>
    public interface IBifurcationOptimizer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        void Optimize(Bifurcation b);
    }
}
