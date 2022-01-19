using System;
using System.Runtime.Serialization;

namespace Vascular.Geometry
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class Matrix3
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public double
            m11 = 1.0, m12 = 0.0, m13 = 0.0,
            m21 = 0.0, m22 = 1.0, m23 = 0.0,
            m31 = 0.0, m32 = 0.0, m33 = 1.0;

        /// <summary>
        /// 
        /// </summary>
        public Matrix3()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        public Matrix3(Matrix3 m) : this(m.m11, m.m12, m.m13, m.m21, m.m22, m.m23, m.m31, m.m32, m.m33)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        public Matrix3(double d) : this(d, d, d, d, d, d, d, d, d)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m11"></param>
        /// <param name="m12"></param>
        /// <param name="m13"></param>
        /// <param name="m21"></param>
        /// <param name="m22"></param>
        /// <param name="m23"></param>
        /// <param name="m31"></param>
        /// <param name="m32"></param>
        /// <param name="m33"></param>
        public Matrix3(double m11, double m12, double m13, double m21, double m22, double m23, double m31, double m32, double m33)
        {
            this.m11 = m11;
            this.m12 = m12;
            this.m13 = m13;
            this.m21 = m21;
            this.m22 = m22;
            this.m23 = m23;
            this.m31 = m31;
            this.m32 = m32;
            this.m33 = m33;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <param name="r3"></param>
        /// <returns></returns>
        public static Matrix3 FromRows(Vector3 r1, Vector3 r2, Vector3 r3)
        {
            return new Matrix3(r1.x, r1.y, r1.z, r2.x, r2.y, r2.z, r3.x, r3.y, r3.z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <param name="c3"></param>
        /// <returns></returns>
        public static Matrix3 FromColumns(Vector3 c1, Vector3 c2, Vector3 c3)
        {
            return new Matrix3(c1.x, c2.x, c3.x, c1.y, c2.y, c3.y, c1.z, c2.z, c3.z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Matrix3 Diagonal(double a)
        {
            return new Matrix3(a, 0, 0, 0, a, 0, 0, 0, a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Matrix3 Diagonal(Vector3 v)
        {
            return new Matrix3(v.x, 0, 0, 0, v.y, 0, 0, 0, v.z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Matrix3 Diagonal(double a, double b, double c)
        {
            return new Matrix3(a, 0, 0, 0, b, 0, 0, 0, c);
        }

        /// <summary>
        /// Returns <paramref name="v"/> <paramref name="v"/>^T
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Matrix3 OuterProduct(Vector3 v)
        {
            var xx = v.x * v.x;
            var yy = v.y * v.y;
            var zz = v.z * v.z;
            var xy = v.x * v.y;
            var yz = v.y * v.z;
            var zx = v.z * v.x;
            return new Matrix3(
                xx, xy, zx,
                xy, yy, yz,
                zx, yz, zz);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Matrix3 OuterProduct(Vector3 u, Vector3 v)
        {
            return new Matrix3(
                u.x * v.x, u.x * v.y, u.x * v.z,
                u.y * v.x, u.y * v.y, u.y * v.z,
                u.z * v.x, u.z * v.y, u.z * v.z);
        }

        /// <summary>
        /// Returns the cross-product matrix for <paramref name="v"/>.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Matrix3 Cross(Vector3 v)
        {
            return new Matrix3(
                0, -v.z, v.y,
                v.z, 0, -v.x,
                -v.y, v.x, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Matrix3 GivensRotationX(double r)
        {
            var s = Math.Sin(r);
            var c = Math.Cos(r);
            return new Matrix3(
                1, 0, 0,
                0, c, -s,
                0, s, c);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Matrix3 GivensRotationY(double r)
        {
            var s = Math.Sin(r);
            var c = Math.Cos(r);
            return new Matrix3(
                c, 0, -s,
                0, 1, 0,
                s, 0, c);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Matrix3 GivensRotationZ(double r)
        {
            var s = Math.Sin(r);
            var c = Math.Cos(r);
            return new Matrix3(
                c, -s, 0,
                s, c, 0,
                0, 0, 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Matrix3 AxisAngleRotation(Vector3 axis, double angle)
        {
            var c = Math.Cos(angle);
            var C = 1.0 - c;
            var s = Math.Sin(angle);
            var x = axis.x;
            var y = axis.y;
            var z = axis.z;
            var x2 = x * x * C;
            var y2 = y * y * C;
            var z2 = z * z * C;
            var xy = x * y * C;
            var yz = y * z * C;
            var zx = z * x * C;
            var sx = s * x;
            var sy = s * y;
            var sz = s * z;
            return new(
                c + x2, xy - sz, zx + sy,
                xy + sz, c + y2, yz - sx,
                zx - sy, yz + sx, c + z2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Matrix3 operator +(Matrix3 l, Matrix3 r)
        {
            return new Matrix3(
                l.m11 + r.m11, l.m12 + r.m12, l.m13 + r.m13,
                l.m21 + r.m21, l.m22 + r.m22, l.m23 + r.m23,
                l.m31 + r.m31, l.m32 + r.m32, l.m33 + r.m33);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Matrix3 operator -(Matrix3 l, Matrix3 r)
        {
            return new Matrix3(
                l.m11 - r.m11, l.m12 - r.m12, l.m13 - r.m13,
                l.m21 - r.m21, l.m22 - r.m22, l.m23 - r.m23,
                l.m31 - r.m31, l.m32 - r.m32, l.m33 - r.m33);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Matrix3 operator *(Matrix3 m, double a)
        {
            return new Matrix3(
                m.m11 * a, m.m12 * a, m.m13 * a,
                m.m21 * a, m.m22 * a, m.m23 * a,
                m.m31 * a, m.m32 * a, m.m33 * a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix3 operator *(double a, Matrix3 m)
        {
            return m * a;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Matrix3 operator /(Matrix3 m, double a)
        {
            return new Matrix3(
                m.m11 / a, m.m12 / a, m.m13 / a,
                m.m21 / a, m.m22 / a, m.m23 / a,
                m.m31 / a, m.m32 / a, m.m33 / a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Matrix3 operator *(Matrix3 l, Matrix3 r)
        {
            return new Matrix3(
                l.m11 * r.m11 + l.m12 * r.m21 + l.m13 * r.m31, l.m11 * r.m12 + l.m12 * r.m22 + l.m13 * r.m32, l.m11 * r.m13 + l.m12 * r.m23 + l.m13 * r.m33,
                l.m21 * r.m11 + l.m22 * r.m21 + l.m23 * r.m31, l.m21 * r.m12 + l.m22 * r.m22 + l.m23 * r.m32, l.m21 * r.m13 + l.m22 * r.m23 + l.m23 * r.m33,
                l.m31 * r.m11 + l.m32 * r.m21 + l.m33 * r.m31, l.m31 * r.m12 + l.m32 * r.m22 + l.m33 * r.m32, l.m31 * r.m13 + l.m32 * r.m23 + l.m33 * r.m33);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 operator *(Matrix3 m, Vector3 v)
        {
            return new Vector3(m.m11 * v.x + m.m12 * v.y + m.m13 * v.z, m.m21 * v.x + m.m22 * v.y + m.m23 * v.z, m.m31 * v.x + m.m32 * v.y + m.m33 * v.z);
        }

        /// <summary>
        /// 
        /// </summary>
        public Matrix3 Transpose => new Matrix3(m11, m21, m31, m12, m22, m32, m13, m23, m33);

        /// <summary>
        /// 
        /// </summary>
        public double Trace => m11 + m22 + m33;

        /// <summary>
        /// 
        /// </summary>
        public double Determinant => m11 * (m22 * m33 - m23 * m32) - m12 * (m21 * m33 - m23 * m31) + m13 * (m21 * m32 - m22 * m31);

        /// <summary>
        /// 
        /// </summary>
        public (Vector3 c1, Vector3 c2, Vector3 c3) Columns => (new Vector3(m11, m21, m31), new Vector3(m12, m22, m32), new Vector3(m13, m23, m33));

        /// <summary>
        /// 
        /// </summary>
        public (Vector3 r1, Vector3 r2, Vector3 r3) Rows => (new Vector3(m11, m12, m13), new Vector3(m21, m22, m23), new Vector3(m31, m32, m33));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="d2">The tolerance for the determinant squared before considered singular.</param>
        /// <returns></returns>
        public Matrix3 Inverse(double d2 = 1.0e-12)
        {
            var (x, y, z) = this.Columns;
            var yz = y ^ z;
            var d = x * yz;
            if (d * d <= d2)
            {
                throw new PhysicalValueException("Attempting to invert singular matrix.");
            }
            var id = 1.0 / d;
            return FromRows(yz, z ^ x, x ^ y) * id;
        }

        /// <summary>
        /// Returns max over columns of absolute sum.
        /// </summary>
        public double NormL1
        {
            get
            {
                var c1 = Math.Abs(m11) + Math.Abs(m21) + Math.Abs(m31);
                var c2 = Math.Abs(m12) + Math.Abs(m22) + Math.Abs(m32);
                var c3 = Math.Abs(m13) + Math.Abs(m23) + Math.Abs(m33);
                return Math.Max(Math.Max(c1, c2), c3);
            }
        }

        /// <summary>
        /// Returns max over rows of absolute sum.
        /// </summary>
        public double NormLInf
        {
            get
            {
                var r1 = Math.Abs(m11) + Math.Abs(m12) + Math.Abs(m13);
                var r2 = Math.Abs(m21) + Math.Abs(m22) + Math.Abs(m23);
                var r3 = Math.Abs(m31) + Math.Abs(m32) + Math.Abs(m33);
                return Math.Max(Math.Max(r1, r2), r3);
            }
        }

        /// <summary>
        /// Returns square root of maximum eigenvalue of this times its own transpose.
        /// </summary>
        public double NormL2
        {
            get
            {
                var lMax = LinearAlgebra.RealSymmetricEigenvalues(this.Transpose * this).Max;
                return Math.Sqrt(lMax);
            }
        }
    }
}
