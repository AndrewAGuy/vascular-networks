using System;
using Vascular.Geometry;

namespace Vascular.Construction.LSC.Defaults
{
    /// <summary>
    /// Simple shapes.
    /// </summary>
    public static class AnalyticExterior
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        public static ExteriorPredicate Cuboid(double X, double Y, double Z)
        {
            X = Math.Abs(X);
            Y = Math.Abs(Y);
            Z = Math.Abs(Z);
            return (z, x) =>
                Math.Abs(x.x) < X &&
                Math.Abs(x.y) < Y &&
                Math.Abs(x.z) < Z;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        public static ExteriorPredicate Octahedron(double X, double Y, double Z)
        {
            X = 1.0 / Math.Abs(X);
            Y = 1.0 / Math.Abs(Y);
            Z = 1.0 / Math.Abs(Z);
            return (z, x) => Math.Abs(x.x) * X + Math.Abs(x.y) * Y + Math.Abs(x.z) * Z < 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static ExteriorPredicate Sphere(double r)
        {
            var r2 = r * r;
            return (z, x) => x.LengthSquared < r2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="R"></param>
        /// <returns></returns>
        public static ExteriorPredicate SphericalShell(double r, double R)
        {
            var r2 = r * r;
            var R2 = R * R;
            (r2, R2) = r2 < R2 ? (r2, R2) : (R2, r2);
            return (z, x) =>
            {
                var x2 = x.LengthSquared;
                return r2 < x2 && x2 < R2;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static ExteriorPredicate Cylinder(double r, double h)
        {
            var r2 = r * r;
            h = Math.Abs(h);
            return (z, x) =>
                x.x * x.x + x.y * x.y < r2 &&
                Math.Abs(x.z) < h;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="R"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static ExteriorPredicate CylindricalShell(double r, double R, double h)
        {
            var r2 = r * r;
            var R2 = R * R;
            (r2, R2) = r2 < R2 ? (r2, R2) : (R2, r2);
            h = Math.Abs(h);
            return (z, x) =>
            {
                var x2 = x.LengthSquared;
                return r2 < x2 && x2 < R2
                    && Math.Abs(x.z) < h;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static ExteriorPredicate HalfSpaceX => (z, x) => x.x > 0;

        /// <summary>
        /// 
        /// </summary>
        public static ExteriorPredicate HalfSpaceY => (z, x) => x.y > 0;

        /// <summary>
        /// 
        /// </summary>
        public static ExteriorPredicate HalfSpaceZ => (z, x) => x.z > 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public static ExteriorPredicate Ellipsoid(Matrix3 A)
        {
            A = A.Inverse();
            return (z, x) =>
            {
                var u = A * x;
                return u * u < 1;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public static ExteriorPredicate EllipsoidShell(Matrix3 a, Matrix3 A)
        {
            (a, A) = Math.Abs(a.Determinant) < Math.Abs(A.Determinant) ? (a, A) : (A, a);
            a = a.Inverse();
            A = A.Inverse();
            return (z, x) =>
            {
                var u = a * x;
                var U = A * x;
                return 1 < u * u && U * U < 1;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static ExteriorPredicate UpperRightTriangularPrism(double X, double Y, double h)
        {
            X = 1.0 / Math.Abs(X);
            Y = 1.0 / Math.Abs(Y);
            h = Math.Abs(h);
            return (z, x) =>
                x.x > 0 && x.y > 0 && x.x * X + x.y * Y < 1 &&
                x.z > 0 && x.z < h;
        }
    }
}
