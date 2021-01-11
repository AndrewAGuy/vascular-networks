using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Structure;

namespace Vascular.Intersections.Enforcement
{
    public interface IIntersectionEvaluator<TIntersection>
    {
        IEnumerable<TIntersection> Evaluate(Network network);
    }
}
