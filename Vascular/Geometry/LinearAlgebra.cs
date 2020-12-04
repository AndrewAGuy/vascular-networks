using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Generators;

namespace Vascular.Geometry
{
    public static class LinearAlgebra
    {
        public static (double A, double B) BranchBranchExtrema(Vector3 da, Vector3 db, double r2)
        {
            var da2 = da * da;
            var db2 = db * db;
            var dot2 = Math.Pow(da * db, 2);
            var frac2 = r2 / (db2 * da2 - dot2);
            var A = Math.Sqrt(frac2 * db2);
            var B = Math.Sqrt(frac2 * da2);
            return (A, B);
        }

        public static Vector3 SolveMatrix3x3(Vector3 col1, Vector3 col2, Vector3 col3, Vector3 v)
        {
            var row1 = col2 ^ col3;
            var row2 = col3 ^ col1;
            var row3 = col1 ^ col2;
            var invdet = 1.0 / (col1 * row1);
            var temp = new Vector3(row1 * v, row2 * v, row3 * v);
            return temp * invdet;
        }

        public static double LineFactor(Vector3 clamp, Vector3 dir, Vector3 target)
        {
            return (target - clamp) * dir / dir.LengthSquared;
        }

        public static (double S, double E) LineFactors(Vector3 clamp, Vector3 dir, Vector3 start, Vector3 end)
        {
            var dot = dir / dir.LengthSquared;
            var cd = clamp * dot;
            return (start * dot - cd, end * dot - cd);
        }

        public static Vector3 RemoveComponent(Vector3 v, Vector3 d)
        {
            return v - d * (v * d);
        }

        public static Vector3 MoveTowardsLine(Vector3 v, Vector3 d, double a)
        {
            var vl = d * (v * d);
            var vp = v - vl;
            return vl + a * vp;
        }

        public static Matrix3 GradientOfDirection(Vector3 v)
        {
            // Gradient of magnitude d(|v|)/dv = v/|v|
            // Gradient of inverse magnitude 1/|v| = -v/|v|^3
            // Diagonal terms: d/dv1 (v1/|v|) = (|v| - v1^2/|v|)/|v|^2 = 1/|v|^3 (|v|^2-v1^2) = 1/|v|^3(v2^2 + v3^2)
            // Off-diagonal terms: d/dvj (vi/|v|) = -vivj/|v|^3
            // Overall: (I tr(vv') - vv')/|v|^3
            var s = Math.Pow(v * v, -1.5);
            var xx = v.x * v.x;
            var yy = v.y * v.y;
            var zz = v.z * v.z;
            var xy = -v.x * v.y;
            var yz = -v.y * v.z;
            var zx = -v.z * v.x;
            var xd = yy + zz;
            var yd = zz + xx;
            var zd = xx + yy;
            return new Matrix3(
                xd, xy, zx,
                xy, yd, yz,
                zx, yz, zd) * s;
        }

        public static Vector3 RealSymmetricEigenvalues(Matrix3 A, double t2 = 1e-12)
        {
            // Solving characteristic eqn for eigenvalues
            // Uses decomposition A = bB + iI where tr(A) placed into iI
            var b_off = Math.Pow(A.m12, 2.0) + Math.Pow(A.m13, 2.0) + Math.Pow(A.m23, 2.0);
            if (b_off < t2)
            {
                var E = Math.Max(Math.Max(A.m11, A.m22), A.m33);
                var e = Math.Min(Math.Min(A.m11, A.m22), A.m33);
                return new Vector3(E, A.Trace - E - e, e);
            }
            else
            {
                var T = A.Trace;
                var i = T / 3.0;
                var b_on = Math.Pow(A.m11 - i, 2.0) + Math.Pow(A.m22 - i, 2.0) + Math.Pow(A.m33 - i, 2.0);
                var b = Math.Sqrt((b_on + 2.0 * b_off) / 6.0);
                var B = (A - Matrix3.Diagonal(i)) / b;
                var d = B.Determinant * 0.5;
                var a = d <= -1 ? Math.PI / 3.0 : d >= 1 ? 0.0 : Math.Acos(d) / 3.0;
                var E = i + 2.0 * b * Math.Cos(a);
                var e = i + 2.0 * b * Math.Cos(a + Math.PI * 2.0 / 3.0);
                return new Vector3(E, T - E - e, e);
            }
        }

        public static Vector3 RealSymmetricEigenvector(Matrix3 A, double e, double t2 = 1e-12)
        {
            // Gets eigenvector associated with eigenvalue e
            // Need 2 independent columns
            var (b1, b2, b3) = (A - Matrix3.Diagonal(e)).Columns;
            if (b1.LengthSquared > t2)
            {
                b1 = b1.Normalize();
                b2 = RemoveComponent(b2, b1);
                if (b2.LengthSquared > t2)
                {
                    return (b1 ^ b2).Normalize();
                }
                b3 = RemoveComponent(b3, b1);
                if (b3.LengthSquared > t2)
                {
                    return (b1 ^ b3).Normalize();
                }
            }
            else if (b2.LengthSquared > t2)
            {
                b2 = b2.Normalize();
                b3 = RemoveComponent(b3, b2);
                if (b3.LengthSquared > t2)
                {
                    return (b2 ^ b3).Normalize();
                }
            }
            return null;
        }

        public static double DistanceToLine(Vector3 a, Vector3 b, Vector3 v)
        {
            var d = b - a;
            var f = LineFactor(a, d, v);
            var p = a + f * d;
            return (v - p).Length;
        }
    }
}
