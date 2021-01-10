using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Intersections.Enforcement
{
    public static class Extensions
    {
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
