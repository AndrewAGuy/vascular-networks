using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vascular.Geometry
{
    public static class Extensions
    {
        public static Vector3 Sum(this IEnumerable<Vector3> V)
        {
            var e = V.GetEnumerator();
            if (!e.MoveNext())
            {
                throw new InvalidOperationException("Cannot sum empty collection.");
            }
            var s = e.Current;
            while (e.MoveNext())
            {
                s += e.Current;
            }
            return s;
        }
    }
}
