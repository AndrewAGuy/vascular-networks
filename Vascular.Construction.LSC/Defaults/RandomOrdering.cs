using System;
using System.Linq;

namespace Vascular.Construction.LSC.Defaults
{
    /// <summary>
    /// Delegates for random ordering of exteriors.
    /// </summary>
    public static class RandomOrdering
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public static ExteriorOrderingGenerator PermuteExterior(Random random)
        {
            return E =>
            {
                var kv = E.ToArray();
                kv.Permute(random);
                return kv;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public static InitialTerminalOrderingGenerator PermuteInitial(Random random)
        {
            return V =>
            {
                var kv = V.ToArray();
                kv.Permute(random);
                return kv;
            };
        }
    }
}
