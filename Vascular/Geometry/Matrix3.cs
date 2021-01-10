using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Vascular.Geometry
{
    [DataContract]
    public class Matrix3
    {
        [DataMember]
        public double 
            m11 = 1.0, m12 = 0.0, m13 = 0.0,
            m21 = 0.0, m22 = 1.0, m23 = 0.0, 
            m31 = 0.0, m32 = 0.0, m33 = 1.0;

        public Matrix3()
        {
        }

        public Matrix3(Matrix3 m) : this(m.m11, m.m12, m.m13, m.m21, m.m22, m.m23, m.m31, m.m32, m.m33)
        {
        }

        public Matrix3(double d) : this(d, d, d, d, d, d, d, d, d)
        {
        }

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

        public static Matrix3 FromRows(Vector3 r1, Vector3 r2, Vector3 r3)
        {
            return new Matrix3(r1.x, r1.y, r1.z, r2.x, r2.y, r2.z, r3.x, r3.y, r3.z);
        }

        public static Matrix3 FromColumns(Vector3 c1, Vector3 c2, Vector3 c3)
        {
            return new Matrix3(c1.x, c2.x, c3.x, c1.y, c2.y, c3.y, c1.z, c2.z, c3.z);
        }

        public static Matrix3 Diagonal(double a)
        {
            return new Matrix3(a, 0, 0, 0, a, 0, 0, 0, a);
        }

        public static Matrix3 Diagonal(Vector3 v)
        {
            return new Matrix3(v.x, 0, 0, 0, v.y, 0, 0, 0, v.z);
        }

        public static Matrix3 Diagonal(double a, double b, double c)
        {
            return new Matrix3(a, 0, 0, 0, b, 0, 0, 0, c);
        }

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

        public static Matrix3 OuterProduct(Vector3 u, Vector3 v)
        {
            return new Matrix3(
                u.x * v.x, u.x * v.y, u.x * v.z,
                u.y * v.x, u.y * v.y, u.y * v.z,
                u.z * v.x, u.z * v.y, u.z * v.z);
        }

        public static Matrix3 Cross(Vector3 v)
        {
            return new Matrix3(
                0, -v.z, v.y,
                v.z, 0, -v.x,
                -v.y, v.x, 0);
        }

        public static Matrix3 GivensRotationX(double r)
        {
            var s = Math.Sin(r);
            var c = Math.Cos(r);
            return new Matrix3(
                1, 0, 0,
                0, c, -s,
                0, s, c);
        }

        public static Matrix3 GivensRotationY(double r)
        {
            var s = Math.Sin(r);
            var c = Math.Cos(r);
            return new Matrix3(
                c, 0, -s,
                0, 1, 0,
                s, 0, c);
        }

        public static Matrix3 GivensRotationZ(double r)
        {
            var s = Math.Sin(r);
            var c = Math.Cos(r);
            return new Matrix3(
                c, -s, 0,
                s, c, 0,
                0, 0, 1);
        }

        public static Matrix3 operator +(Matrix3 l, Matrix3 r)
        {
            return new Matrix3(
                l.m11 + r.m11, l.m12 + r.m12, l.m13 + r.m13,
                l.m21 + r.m21, l.m22 + r.m22, l.m23 + r.m23,
                l.m31 + r.m31, l.m32 + r.m32, l.m33 + r.m33);
        }

        public static Matrix3 operator -(Matrix3 l, Matrix3 r)
        {
            return new Matrix3(
                l.m11 - r.m11, l.m12 - r.m12, l.m13 - r.m13,
                l.m21 - r.m21, l.m22 - r.m22, l.m23 - r.m23,
                l.m31 - r.m31, l.m32 - r.m32, l.m33 - r.m33);
        }

        public static Matrix3 operator *(Matrix3 m, double a)
        {
            return new Matrix3(
                m.m11 * a, m.m12 * a, m.m13 * a,
                m.m21 * a, m.m22 * a, m.m23 * a,
                m.m31 * a, m.m32 * a, m.m33 * a);
        }

        public static Matrix3 operator *(double a, Matrix3 m)
        {
            return m * a;
        }

        public static Matrix3 operator /(Matrix3 m, double a)
        {
            return new Matrix3(
                m.m11 / a, m.m12 / a, m.m13 / a,
                m.m21 / a, m.m22 / a, m.m23 / a,
                m.m31 / a, m.m32 / a, m.m33 / a);
        }

        public static Matrix3 operator *(Matrix3 l, Matrix3 r)
        {
            return new Matrix3(
                l.m11 * r.m11 + l.m12 * r.m21 + l.m13 * r.m31, l.m11 * r.m12 + l.m12 * r.m22 + l.m13 * r.m32, l.m11 * r.m13 + l.m12 * r.m23 + l.m13 * r.m33,
                l.m21 * r.m11 + l.m22 * r.m21 + l.m23 * r.m31, l.m21 * r.m12 + l.m22 * r.m22 + l.m23 * r.m32, l.m21 * r.m13 + l.m22 * r.m23 + l.m23 * r.m33,
                l.m31 * r.m11 + l.m32 * r.m21 + l.m33 * r.m31, l.m31 * r.m12 + l.m32 * r.m22 + l.m33 * r.m32, l.m31 * r.m13 + l.m32 * r.m23 + l.m33 * r.m33);
        }

        public static Vector3 operator *(Matrix3 m, Vector3 v)
        {
            return new Vector3(m.m11 * v.x + m.m12 * v.y + m.m13 * v.z, m.m21 * v.x + m.m22 * v.y + m.m23 * v.z, m.m31 * v.x + m.m32 * v.y + m.m33 * v.z);
        }

        public Matrix3 Transpose => new Matrix3(m11, m21, m31, m12, m22, m32, m13, m23, m33);

        public double Trace => m11 + m22 + m33;

        public double Determinant => m11 * (m22 * m33 - m23 * m32) - m12 * (m21 * m33 - m23 * m31) + m13 * (m21 * m32 - m22 * m31);

        public (Vector3 c1, Vector3 c2, Vector3 c3) Columns => (new Vector3(m11, m21, m31), new Vector3(m12, m22, m32), new Vector3(m13, m23, m33));

        public (Vector3 r1, Vector3 r2, Vector3 r3) Rows => (new Vector3(m11, m12, m13), new Vector3(m21, m22, m23), new Vector3(m31, m32, m33));

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
    }
}
