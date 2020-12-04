using System;

namespace Vascular.Geometry
{
    [Serializable]
    public class Plane3
    {
        public double x = 0.0;
        public double y = 0.0;
        public double z = 0.0;
        public double d = 0.0;

        public Plane3()
        {
        }

        public Plane3(Vector3 direction, double distance)
        {
            var n = direction.Normalize();
            x = n.x;
            y = n.y;
            z = n.z;
            d = distance;
        }

        public Plane3(Vector3 point)
        {
            d = point.Length;
            x = point.x / d;
            y = point.y / d;
            z = point.z / d;
        }

        public double Distance(Vector3 v)
        {
            return v.x * x + v.y * y + v.z * z - d;
        }
    }
}
