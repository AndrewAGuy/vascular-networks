using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Executes <paramref name="steps"/> iterations of each <see cref="IEnforcer"/> in <paramref name="enforcers"/> until all return no violations.
        /// </summary>
        /// <param name="enforcers"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        public static async Task IterateToEnd(this IEnumerable<IEnforcer> enforcers, int steps)
        {
            IEnforcer firstPassing = null;
            while (true)
            {
                foreach (var enforcer in enforcers)
                {
                    if (enforcer == firstPassing)
                    {
                        return;
                    }

                    if (await enforcer.Advance(steps) == 0)
                    {
                        firstPassing ??= enforcer;
                    }
                    else
                    {
                        firstPassing = null;
                    }
                }
            }
        }
    }
}
