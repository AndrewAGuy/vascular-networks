namespace Vascular.Geometry
{
    /// <summary>
    ///
    /// </summary>
    public class Plane3
    {
        /// <summary>
        ///
        /// </summary>
        public double x = 0.0, y = 0.0, z = 0.0, d = 0.0;

        /// <summary>
        ///
        /// </summary>
        public Plane3()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        public Plane3(Vector3 direction, double distance)
        {
            var n = direction.Normalize();
            x = n.x;
            y = n.y;
            z = n.z;
            d = distance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="point"></param>
        public Plane3(Vector3 point)
        {
            d = point.Length;
            x = point.x / d;
            y = point.y / d;
            z = point.z / d;
        }

        /// <summary>
        /// Distance from point to plane.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double Distance(Vector3 v)
        {
            return v.x * x + v.y * y + v.z * z - d;
        }
    }
}
