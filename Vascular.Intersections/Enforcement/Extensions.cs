using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vascular.Intersections.Enforcement
{
    public static class Extensions
    {
        public static void IterateToEnd(this IEnumerable<IEnforcer> enforcers, int steps)
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

                    if (enforcer.Advance(steps) == 0)
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
