using System;
using System.Collections.Generic;
using System.Text;

namespace Vascular.Geometry.Bounds
{
    public static class AxialBoundsExtensions
    {
        public static AxialBounds GetTotalBounds<T>(this IEnumerable<T> es) where T : IAxialBoundable
        {
            var e = es.GetEnumerator();
            var b = new AxialBounds();
            while (e.MoveNext())
            {
                b.Append(e.Current.GetAxialBounds());
            }
            return b;
        }
    }
}
