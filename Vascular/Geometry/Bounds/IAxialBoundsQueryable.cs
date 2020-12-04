using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Bounds
{
    public interface IAxialBoundsQueryable<T> : IEnumerable<T> where T : IAxialBoundable
    {
        void Query(AxialBounds query, Action<T> action);
    }
}
