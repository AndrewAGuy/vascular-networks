using System.Collections.Generic;
using Vascular.Structure;

namespace Vascular.Intersections.Enforcement
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TIntersection"></typeparam>
    public interface IIntersectionEvaluator<TIntersection>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        IEnumerable<TIntersection> Evaluate(Network network);
    }
}
