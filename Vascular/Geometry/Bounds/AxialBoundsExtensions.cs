using System.Collections.Generic;
using Vascular.Geometry.Triangulation;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// 
    /// </summary>
    public static class AxialBoundsExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IAxialBoundsQueryable<T> AsQueryable<T>(this IEnumerable<T> e) where T : IAxialBoundable
        {
            return new AxialBoundsQuerySequence<T>(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="es"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Converts bounds to a series of points and multiples by <paramref name="m"/>.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static AxialBounds Transform(this AxialBounds b, Matrix3 m)
        {
            var l = b.Lower;
            var u = b.Upper;
            var v000 = l;
            var v001 = new Vector3(l.x, l.y, u.z);
            var v010 = new Vector3(l.x, u.y, l.z);
            var v100 = new Vector3(u.x, l.y, l.z);
            var v011 = new Vector3(l.x, u.y, u.z);
            var v110 = new Vector3(u.x, u.y, l.z);
            var v101 = new Vector3(u.x, l.y, u.z);
            var v111 = u;
            return new AxialBounds(m * v000).Append(m * v001).Append(m * v010).Append(m * v011)
                .Append(m * v100).Append(m * v101).Append(m * v110).Append(m * v111);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3[] Vertices(this AxialBounds b)
        {
            var v000 = new Vector3(b.Lower);
            var v111 = new Vector3(b.Upper);
            var v001 = new Vector3(v000.x, v000.y, v111.z);
            var v010 = new Vector3(v000.x, v111.y, v000.z);
            var v100 = new Vector3(v111.x, v000.y, v000.z);
            var v011 = new Vector3(v000.x, v111.y, v111.z);
            var v110 = new Vector3(v111.x, v111.y, v000.z);
            var v101 = new Vector3(v111.x, v000.y, v111.z);
            return new Vector3[8] { v000, v001, v010, v011, v100, v101, v110, v111 };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Mesh ToMesh(this AxialBounds b)
        {
            var m = new Mesh();
            var v000 = b.Lower;
            var v111 = b.Upper;
            var v001 = new Vector3(v000.x, v000.y, v111.z);
            var v010 = new Vector3(v000.x, v111.y, v000.z);
            var v100 = new Vector3(v111.x, v000.y, v000.z);
            var v011 = new Vector3(v000.x, v111.y, v111.z);
            var v110 = new Vector3(v111.x, v111.y, v000.z);
            var v101 = new Vector3(v111.x, v000.y, v111.z);

            m.AddTriangle(v001, v101, v111); // +Z
            m.AddTriangle(v001, v111, v011);
            m.AddTriangle(v000, v110, v100); // -Z
            m.AddTriangle(v000, v010, v110);

            m.AddTriangle(v100, v110, v111); // +X
            m.AddTriangle(v100, v111, v101);
            m.AddTriangle(v000, v011, v010); // -X
            m.AddTriangle(v000, v001, v011);

            m.AddTriangle(v110, v010, v011); // +Y
            m.AddTriangle(v110, v011, v111);
            m.AddTriangle(v100, v001, v000); // -Y
            m.AddTriangle(v100, v101, v001);

            return m;
        }
    }
}
