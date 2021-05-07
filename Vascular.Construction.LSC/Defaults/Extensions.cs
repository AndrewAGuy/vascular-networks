using Vascular.Geometry.Lattices.Manipulation;

namespace Vascular.Construction.LSC.Defaults
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static InitialTerminalPredicate AsInitialTerminalPredicate(this ExteriorPredicate e, ClosestBasisFunction f)
        {
            return (S, T) => e(f(T), T);
        }
    }
}
