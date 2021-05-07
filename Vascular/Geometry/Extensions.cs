using System;
using System.Collections.Generic;

namespace Vascular.Geometry
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
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
