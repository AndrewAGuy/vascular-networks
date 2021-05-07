using System;
using System.Collections.Generic;
using Vascular.Geometry;

namespace Vascular.Construction.LSC.Defaults
{
    /// <summary>
    /// Some schemes, particularly when running trimming actions as a <see cref="LatticeState.AfterSpreadAction"/>, 
    /// can cause an infinite loop in which neighbouring exterior vectors with invalid candidate interior connections
    /// are added and immediately removed. This causes the exterior to propagate back and forth between them.
    /// <para/>
    /// This class creates a set of delegates that track the number of times an exterior vector has been visisted, and
    /// prevents it from being readded more than a given number of times. Can conflict with <see cref="SingleBuild"/>
    /// delegates, which might require the limit to be raised.
    /// </summary>
    public class ExteriorLimiter
    {
        /// <summary>
        /// Clears the tracked positions. Run this on <see cref="LatticeState.AfterCoarsenAction"/>, 
        /// <see cref="LatticeState.AfterRefineAction"/>, <see cref="LatticeState.AfterReRefineAction"/>.
        /// </summary>
        public Action OnEntry { get; }

        /// <summary>
        /// Tests the integral vector argument against the visited map. If limit is hit, returns false.
        /// </summary>
        public ExteriorPredicate Predicate { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="capacity">The initial map capacity.</param>
        public ExteriorLimiter(int limit, int capacity = 2048)
        {
            var visited = new Dictionary<Vector3, int>(capacity);
            this.OnEntry = () => visited.Clear();            
            this.Predicate = (z, x) =>
            {
                if (visited.TryGetValue(z, out var n))
                {
                    if (n >= limit)
                    {
                        return false;
                    }
                    visited[z] = n + 1;
                }
                else
                {
                    visited[z] = 1;
                }
                return true;
            };
        }
    }
}
