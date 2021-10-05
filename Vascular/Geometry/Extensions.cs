using System;
using System.Collections.Generic;
using Vascular.Geometry.Generators;

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

        /// <summary>
        /// For a ball of radius <paramref name="r"/> centred at <paramref name="x0"/>, return either <paramref name="x"/>
        /// if inside the ball, or the closest point on the surface if not.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Vector3 ClampToBall(this Vector3 x, Vector3 x0, double r)
        {
            var d = x - x0;
            var d2 = d.LengthSquared;
            return d2 <= r * r
                ? x
                : x0 + d * (r / Math.Sqrt(d2));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x0"></param>
        /// <param name="r"></param>
        /// <param name="t2"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        public static Vector3 ClampOutsideBall(this Vector3 x, Vector3 x0, double r, 
            double t2 = 1e-12, IVector3Generator generator = null)
        {
            var d = x - x0;
            var d2 = d.LengthSquared;
            if (d2 >= r * r)
            {
                return x;
            }
            else if (d2 <= t2)
            {
                var v = generator?.NextVector3().NormalizeSafe(t2) ?? Vector3.UNIT_Z;
                return x0 + v * r;
            }
            else
            {
                return x0 + d * (r / Math.Sqrt(d2));
            }
        }
    }
}
