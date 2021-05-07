using System;
using System.Runtime.CompilerServices;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Triangulation;

namespace Vascular.Geometry.Surfaces
{
    /// <summary>
    /// Fast surface testing by caching.
    /// </summary>
    public class TriangleSurfaceTest : IAxialBoundable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="r"></param>
        /// <param name="t2"></param>
        public TriangleSurfaceTest(Triangle tr, double r = 0.0, double t2 = 1.0e-12) : this(tr.A.P, tr.B.P, tr.C.P, r, t2)
        {
            tri = tr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="r"></param>
        /// <param name="t2"></param>
        public TriangleSurfaceTest(Vector3 A, Vector3 B, Vector3 C, double r = 0.0, double t2 = 1.0e-12)
        {
            // Copy position vectors, calculate edges
            p0 = new Vector3(A);
            p1 = new Vector3(B);
            p2 = new Vector3(C);
            e01 = p1 - p0;
            e02 = p2 - p0;
            e12 = p2 - p1;
            eu01 = e01.Normalize();
            eu02 = e02.Normalize();
            eu12 = e12.Normalize();
            p0e01 = p0 * eu01;
            p0e02 = p0 * eu02;
            p1e01 = p1 * eu01;
            p1e12 = p1 * eu12;
            p2e02 = p2 * eu02;
            p2e12 = p2 * eu12;
            // Is the normal vector ok? Get the plane of this triangle with unit normal
            this.t2 = t2;
            var nn = e01 ^ e02;
            var m2 = nn * nn;
            if (m2 <= t2)
            {
                throw new GeometryException($"Triangle normal too small: {{ [{p0}] [{p1}] [{p2}] }} has square magnitude {m2}");
            }
            n = nn / Math.Sqrt(m2);
            d = p0 * n;
            // Generate the basis for testing if inside the triangle and bounding box
            b01 = (e02 ^ nn) / m2;
            b02 = (nn ^ e01) / m2;
            ab = new AxialBounds(p0).Append(p1).Append(p2).Extend(r);
        }

        private readonly Triangle tri;
        private readonly Vector3 p0, p1, p2;
        private readonly Vector3 e01, e02, e12;
        private readonly Vector3 eu01, eu02, eu12;
        private readonly double p0e01, p0e02, p1e01, p1e12, p2e02, p2e12;
        private readonly Vector3 n;
        private readonly double d;
        private readonly AxialBounds ab;
        private readonly Vector3 b01, b02;
        private readonly double t2;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 A => p0;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 B => p1;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 C => p2;

        /// <summary>
        /// 
        /// </summary>
        public Triangle Triangle => tri;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Normal => n;

        /// <summary>
        /// The key method, used for testing segment intersections with meshes.
        /// </summary>
        /// <param name="r0"></param>
        /// <param name="rd"></param>
        /// <param name="t"></param>
        /// <param name="f"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool TestRay(Vector3 r0, Vector3 rd, double t, ref double f, ref Vector3 p)
        {
            // We want the first point of contact. Test if we start the ray above or below the testing planes, and if the ray moves away.
            var r0n = d - r0 * n;
            var rdn = rd * n;
            if (r0n < -t)
            {
                if (rdn >= 0)
                {
                    return false;
                }
                // Test top plane
                f = (r0n + t) / rdn;
                if (f > 1)
                {
                    return false;
                }
                p = r0 + f * rd;
            }
            else if (r0n > t)
            {
                if (rdn <= 0)
                {
                    return false;
                }
                // Test bottom plane
                f = (r0n - t) / rdn;
                if (f > 1)
                {
                    return false;
                }
                p = r0 + f * rd;
            }
            else
            {
                // r0 is between the planes - edge projected ray direction can be 0
                f = 0;
                p = r0;
            }
            // Triangle testing - express rp - p0 = a01 * e01 + a02 * e02 ( + 0 * n)   
            var rp0 = p - p0;
            var a01 = rp0 * b01;
            var a02 = rp0 * b02;
            if (a01 >= 0)
            {
                if (a02 >= 0)
                {
                    if (a01 + a02 <= 1)
                    {
                        return true;
                    }
                    else
                    {
                        // Test 12
                        return TestRayEdge(p1, p2, eu12, p1e12, p2e12, r0, rd, t * t, ref f, ref p);
                    }
                }
                else
                {
                    // We are not inside, and need to test 01.
                    if (a01 + a02 <= 1)
                    {
                        // Test only 01
                        return TestRayEdge(p0, p1, eu01, p0e01, p1e01, r0, rd, t * t, ref f, ref p);
                    }
                    else
                    {
                        // Test 01, 12
                        return TestRayEdgePair(
                            p0, p1, eu01, p0e01, p1e01,
                            p1, p2, eu12, p1e12, p2e12,
                            r0, rd, t * t, ref f, ref p);
                    }
                }
            }
            else
            {
                // We are not inside, and need to test 02.
                if (a02 >= 0)
                {
                    if (a01 + a02 <= 1)
                    {
                        // Test only 02
                        return TestRayEdge(p0, p2, eu02, p0e02, p2e02, r0, rd, t * t, ref f, ref p);
                    }
                    else
                    {
                        // Test 02, 12
                        return TestRayEdgePair(
                            p0, p2, eu02, p0e02, p2e02,
                            p1, p2, eu12, p1e12, p2e12,
                            r0, rd, t * t, ref f, ref p);
                    }
                }
                else
                {
                    // Test 01, 02
                    return TestRayEdgePair(
                        p0, p1, eu01, p0e01, p1e01,
                        p0, p2, eu02, p0e02, p2e02,
                        r0, rd, t * t, ref f, ref p);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TestRayEdgePair(
            Vector3 e0a, Vector3 e1a, Vector3 euda, double d0a, double d1a,
            Vector3 e0b, Vector3 e1b, Vector3 eudb, double d0b, double d1b,
            Vector3 r0, Vector3 rd, double t2, ref double f, ref Vector3 p)
        {
            Vector3 pa = null, pb = null;
            double fa = 0.0, fb = 0.0;
            var ba = TestRayEdge(e0a, e1a, euda, d0a, d1a, r0, rd, t2, ref fa, ref pa);
            var bb = TestRayEdge(e0b, e1b, eudb, d0b, d1b, r0, rd, t2, ref fb, ref pb);
            if (ba)
            {
                if (bb)
                {
                    if (fa < fb)
                    {
                        f = fa;
                        p = pa;
                        return true;
                    }
                    f = fb;
                    p = pb;
                    return true;
                }
                f = fa;
                p = pa;
                return true;
            }
            if (bb)
            {
                f = fb;
                p = pb;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TestRayEdge(
            Vector3 e0, Vector3 e1, Vector3 eud, double d0, double d1,
            Vector3 r0, Vector3 rd, double t2, ref double f, ref Vector3 p)
        {
            // Convert ray clamp into e0 centred coords
            var r0e = r0 - e0;
            // Given unit direction of edge, project down into edge plane
            var r0p = r0e - (r0e * eud) * eud;
            var rdp = rd - (rd * eud) * eud;
            // Solve quadratic: |r0p+f*rdp|=t, can remove factors of 2 here in solution
            // If the ray projects to a point, we need to test differently.
            var a = rdp * rdp;
            var c = r0p * r0p - t2;
            if (a <= this.t2)
            {
                // Quadratic invalid, both a and b will be 0. Start off by testing if we're outside radius
                if (c > 0)
                {
                    return false;
                }
                // In this case, we want to find the range of f which the edge covers
                double f0 = 0.0, f1 = 0.0;
                Vector3 p0 = null, p1 = null;
                var b0 = TestRayVertex(e0, r0, rd, t2, ref f0, ref p0);
                var b1 = TestRayVertex(e1, r0, rd, t2, ref f1, ref p1);
                if (b0)
                {
                    if (b1)
                    {
                        if (f0 < f1)
                        {
                            f = f0;
                            p = p0;
                            return true;
                        }
                        f = f1;
                        p = p1;
                        return true;
                    }
                    f = f0;
                    p = p0;
                    return true;
                }
                if (b1)
                {
                    f = f1;
                    p = p1;
                    return true;
                }
                return false;
            }
            else
            {
                var b = rdp * r0p;
                var d = b * b - a * c;
                if (d < 0)
                {
                    return false;
                }
                d = Math.Sqrt(d);
                // If intersection range begins after ray end or ends before ray start, cannot intersect
                var f0 = (-b - d) / a;
                if (f0 > 1)
                {
                    return false;
                }
                var f1 = (-b + d) / a;
                if (f1 < 0)
                {
                    return false;
                }
                // Keep the lower intersection point, clamped to the start of the ray
                f = f0 < 0 ? 0 : f0;
                p = r0 + f * rd;
                // Is this point above/below the cylinder?
                var peud = p * eud;
                if (peud > d1)
                {
                    // Test end point
                    return TestRayVertex(e1, r0, rd, t2, ref f, ref p);
                }
                if (peud < d0)
                {
                    // Test start point
                    return TestRayVertex(e0, r0, rd, t2, ref f, ref p);
                }
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TestRayVertex(Vector3 v, Vector3 r0, Vector3 rd, double t2, ref double f, ref Vector3 p)
        {
            // Solving |r0+f*rd-v|=t2
            var r0v = r0 - v;
            var a = rd * rd;
            var b = rd * r0v;
            var c = r0v * r0v - t2;
            // Solving the same thing as in cylinder test
            var d = b * b - a * c;
            if (d < 0)
            {
                return false;
            }
            d = Math.Sqrt(d);
            // If intersection range begins after ray end or ends before ray start, cannot intersect
            var f0 = (-b - d) / a;
            if (f0 > 1)
            {
                return false;
            }
            var f1 = (-b + d) / a;
            if (f1 < 0)
            {
                return false;
            }
            f = f0 < 0 ? 0 : f0;
            p = r0 + f * rd;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AxialBounds GetAxialBounds()
        {
            return ab;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double DistanceToPlane(Vector3 v)
        {
            return v * n - d;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector3 ProjectToPlane(Vector3 v)
        {
            // v = p + a * n
            // p.n = d, --> v.n = d + a
            var f = v * n - d;
            return v - f * n;
        }

        /// <summary>
        /// Closest point in triangle to <paramref name="p"/>, returns distance <paramref name="d"/>.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public Vector3 ClosestPoint(Vector3 p, out double d)
        {
            var inPlane = ProjectToPlane(p);
            var toInPlane = inPlane - p0;
            var a01 = b01 * toInPlane;
            var a02 = b02 * toInPlane;
            if (a01 >= 0)
            {
                if (a02 >= 0)
                {
                    if (a01 + a02 <= 1)
                    {
                        // Inside
                        d = Vector3.Distance(p, inPlane);
                        return inPlane;
                    }
                    else
                    {
                        // Beyond edge 1-2
                        var cp = p1 + e12 * LinearAlgebra.LineFactor(p1, e12, inPlane).Clamp(0, 1);
                        d = Vector3.Distance(cp, p);
                        return cp;
                    }
                }
                else
                {
                    // Beyond edge 0-1
                    var cp = p0 + e01 * LinearAlgebra.LineFactor(p0, e01, inPlane).Clamp(0, 1);
                    d = Vector3.Distance(cp, p);
                    return cp;
                }
            }
            else
            {
                // Beyond edge 0-2
                var cp = p0 + e02 * LinearAlgebra.LineFactor(p0, e02, inPlane).Clamp(0, 1);
                d = Vector3.Distance(cp, p);
                return cp;
            }
        }

        /// <summary>
        /// Distance from triangle to point.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double DistanceSquared(Vector3 v)
        {
            var inPlane = ProjectToPlane(v);
            var toInPlane = inPlane - p0;
            var a01 = b01 * toInPlane;
            var a02 = b02 * toInPlane;
            if (a01 >= 0)
            {
                if (a02 >= 0)
                {
                    if (a01 + a02 <= 1)
                    {
                        // Inside
                        return Vector3.DistanceSquared(v, inPlane);
                    }
                    else
                    {
                        // Beyond edge 1-2
                        var cp = p1 + e12 * LinearAlgebra.LineFactor(p1, e12, inPlane).Clamp(0, 1);
                        return Vector3.DistanceSquared(v, cp);
                    }
                }
                else
                {
                    // Beyond edge 0-1
                    var cp = p0 + e01 * LinearAlgebra.LineFactor(p0, e01, inPlane).Clamp(0, 1);
                    return Vector3.DistanceSquared(v, cp);
                }
            }
            else
            {
                // Beyond edge 0-2
                var cp = p0 + e02 * LinearAlgebra.LineFactor(p0, e02, inPlane).Clamp(0, 1);
                return Vector3.DistanceSquared(v, cp);
            }
        }

        /// <summary>
        /// Honestly I'm not sure why I wrote this now.
        /// </summary>
        /// <param name="clamp"></param>
        /// <param name="direction"></param>
        /// <param name="position"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public bool Extremum(Vector3 clamp, Vector3 direction, out Vector3 position, out Vector3 normal)
        {
            // Work in edge coords
            var clampAdjust = clamp - p0;
            var cA = b01 * clampAdjust;
            var cB = b02 * clampAdjust;
            var dA = b01 * direction;
            var dB = b02 * direction;
            var dD = dA + dB;

            // Now want to find extrema: intersections of A=0; B=0; A+B=1. There must be intersections with at least two lines, or none.
            var zA = dA * dA <= t2;
            var zB = dB * dB <= t2;
            var zD = dD * dD <= t2;
            if (zA)
            {
                if (zB)
                {
                    position = null;
                    normal = null;
                    return false;
                }
                // We cannot reach A=0 with this line - test B=0; A+B=1
                var fB = -cB / dB;
                // We now want to solve:
                //  cA + fAB * dA = a'
                //  cB + fAB * dB = 1 - a'
                // Giving the matrix problem:
                //  [ dA  -1 ][ fAB ]   [ -cA  ]
                //  [ dB   1 ][  a' ] = [ 1-cB ]
                // The condition for solution is that dA + dB != 0, but in this case we have asserted that dA ~ 0 so dB !~ 0 guarantees this.
                // Inverse of 2x2: [a b; c d] <=> 1/det[d -b; -c a]
                var fAB = (1 - cA - cB) / dD;
                if (fAB > fB)
                {
                    position = clamp + fAB * direction;
                    normal = tri.BC.GetNormal();
                }
                else
                {
                    position = clamp + fB * direction;
                    normal = tri.CA.GetNormal();
                }
            }
            else if (zB)
            {
                var fA = -cA / dA;
                var fAB = (1 - cA - cB) / dD;
                if (fAB > fA)
                {
                    position = clamp + fAB * direction;
                    normal = tri.BC.GetNormal();
                }
                else
                {
                    position = clamp + fA * direction;
                    normal = tri.AB.GetNormal();
                }
            }
            else if (zD)
            {
                var fA = -cA / dA;
                var fB = -cB / dB;
                if (fA > fB)
                {
                    position = clamp + fA * direction;
                    normal = tri.AB.GetNormal();
                }
                else
                {
                    position = clamp + fB * direction;
                    normal = tri.CA.GetNormal();
                }
            }
            else
            {
                var fA = -cA / dA;
                var fB = -cB / dB;
                var fAB = (1 - cA - cB) / dD;
                if (fA > fB)
                {
                    if (fAB > fA)
                    {
                        position = clamp + fAB * direction;
                        normal = tri.BC.GetNormal();
                    }
                    else
                    {
                        position = clamp + fA * direction;
                        normal = tri.AB.GetNormal();
                    }
                }
                else
                {
                    if (fAB > fB)
                    {
                        position = clamp + fAB * direction;
                        normal = tri.BC.GetNormal();
                    }
                    else
                    {
                        position = clamp + fB * direction;
                        normal = tri.CA.GetNormal();
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Test a triangle against another naiively.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool TestTriangleRays(TriangleSurfaceTest other, out Vector3 a, out Vector3 b)
        {
            var f = 0.0;
            Vector3 p = null;
            a = null;
            b = null;
            
            if (TestRay(other.p0, other.e01, 0, ref f, ref p))
            {
                TryAssign(ref a, ref b, p);
            }
            if (TestRay(other.p1, other.e12, 0, ref f, ref p))
            {
                TryAssign(ref a, ref b, p);
            }
            if (TestRay(other.p0, other.e02, 0, ref f, ref p))
            {
                TryAssign(ref a, ref b, p);
            }

            if (other.TestRay(p0, e01, 0, ref f, ref p))
            {
                TryAssign(ref a, ref b, p);
            }
            if (other.TestRay(p1, e12, 0, ref f, ref p))
            {
                TryAssign(ref a, ref b, p);
            }
            if (other.TestRay(p0, e02, 0, ref f, ref p))
            {
                TryAssign(ref a, ref b, p);
            }

            return a != null && (b != null ? true : throw new GeometryException("Ray intersection tests must find 2 intersection points"));
        }

        private static void TryAssign(ref Vector3 a, ref Vector3 b, Vector3 v)
        {
            if (a == null)
            {
                a = v;
            }
            else if (b == null)
            {
                b = v;
            }
            else
            {
                throw new GeometryException("Ray intersection tests can only return 2 intersection points");
            }
        }
    }
}
