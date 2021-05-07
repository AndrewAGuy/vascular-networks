using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Generators;
using Vascular.Geometry.Lattices;
using Vascular.Geometry.Surfaces;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO
{
    /// <summary>
    /// Methods for creating terminal layouts in simple geometries and some more complex.
    /// </summary>
    public static class TerminalLayout
    {
        /// <summary>
        /// A cuboid with equal spacing and flow rates.
        /// </summary>
        /// <param name="count">The number of terminals in each direction.</param>
        /// <param name="flow">The flow rate at each terminal.</param>
        /// <param name="spacing">The terminal spacing.</param>
        /// <param name="centred">Whether to shift back from the positive orthant to be centred around the origin.</param>
        /// <param name="offset">Offset applied to each terminal.</param>
        /// <returns></returns>
        public static Terminal[] UniformCuboidal(Vector3 count, double flow, double spacing, bool centred, Vector3 offset)
        {
            var x = (int)count.x;
            var y = (int)count.y;
            var z = (int)count.z;
            var d = spacing;
            var Q = flow;

            var l = new Vector3(x - 1, y - 1, z - 1) * d;
            var s = (centred ? -l * 0.5 : new Vector3()) + offset;
            var t = new Terminal[x * y * z];
            var n = 0;
            for (var i = 0; i < x; ++i)
            {
                for (var j = 0; j < y; ++j)
                {
                    for (var k = 0; k < z; ++k)
                    {
                        var p = new Vector3(i, j, k) * d;
                        t[n++] = new Terminal(s + p, Q);
                    }
                }
            }

            return t;
        }

        /// <summary>
        /// A cuboid with nonequal spacing and equal flow rates.
        /// </summary>
        /// <param name="count">The number of terminals in each direction.</param>
        /// <param name="flow">The flow rate at each terminal.</param>
        /// <param name="spacing">The terminal spacing in each direction.</param>
        /// <param name="centred">Whether to shift back from the positive orthant to be centred around the origin.</param>
        /// <param name="offset">Offset applied to each terminal.</param>
        /// <returns></returns>
        public static Terminal[] NonUniformCuboidal(Vector3 count, double flow, Vector3 spacing, bool centred, Vector3 offset)
        {
            var x = (int)count.x;
            var y = (int)count.y;
            var z = (int)count.z;
            var d = spacing;
            var Q = flow;

            var l = new Vector3((x - 1) * d.x, (y - 1) * d.y, (z - 1) * d.z);
            var s = (centred ? -l * 0.5 : new Vector3()) + offset;
            var t = new Terminal[x * y * z];
            var n = 0;
            for (var i = 0; i < x; ++i)
            {
                for (var j = 0; j < y; ++j)
                {
                    for (var k = 0; k < z; ++k)
                    {
                        var p = new Vector3(i * d.x, j * d.y, k * d.z);
                        t[n++] = new Terminal(s + p, Q);
                    }
                }
            }

            return t;
        }

        /// <summary>
        /// Creates a triangular prism with uniform spacing and flow rate.
        /// </summary>
        /// <param name="count">The number of terminals in each direction.</param>
        /// <param name="flow">The flow rate at each terminal.</param>
        /// <param name="spacing">The terminal spacing.</param>
        /// <param name="flip_x">Whether to flip in the x direction.</param>
        /// <param name="flip_y">Whether to flip in the y direction.</param>
        /// <param name="round">Whether to round the edges by removing terminals.</param>
        /// <returns></returns>
        public static Terminal[] UniformTriangular(Vector3 count, double flow, double spacing, bool flip_x, bool flip_y, bool round)
        {
            var x = count.x;
            var ix = (int)x;
            var y = count.y;
            var iz = (int)count.z;

            var t = new List<Terminal>();
            var r = y / x;
            var xu = ix - 1;
            var zu = iz - 1;

            var fy = flip_y ? -1.0 : 1.0;
            var fx = flip_x ? -1.0 : 1.0;
            for (var i = 0; i < ix; ++i)
            {
                var h = Math.Round((ix - i) * r);
                var yu = h - 1;
                for (var j = 0; j < h; ++j)
                {
                    for (var k = 0; k < iz; ++k)
                    {
                        if (round)
                        {
                            if (i == 0 || i == xu)
                            {
                                if (j == 0 || j == yu || k == 0 || k == zu)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (j == 0 || j == yu)
                                {
                                    if (k == 0 || k == zu)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        var p = new Vector3(i * fx, j * fy, k) * spacing;
                        t.Add(new Terminal(p, flow));
                    }
                }
            }

            return t.ToArray();
        }

        /// <summary>
        /// Fills a cylinder with points on a cubic lattice.
        /// </summary>
        /// <param name="radius">The cylinder radius.</param>
        /// <param name="height">The cylinder height.</param>
        /// <param name="flow">The terminal flow rate.</param>
        /// <param name="spacing">The terminal spacing.</param>
        /// <param name="offset">Offset applied to each terminal.</param>
        /// <returns></returns>
        public static Terminal[] UniformCylindrical(double radius, double height, double flow, double spacing, Vector3 offset)
        {
            var l = new List<Terminal>();
            var w = (int)Math.Floor(radius / spacing);
            var h = (int)Math.Floor(height / spacing);
            var d = (height - h * spacing) / 2;
            for (var x = -w; x <= w; ++x)
            {
                for (var y = -w; y <= w; ++y)
                {
                    for (var z = 0; z <= h; ++z)
                    {
                        var v = new Vector3(x * spacing, y * spacing, z * spacing + d);
                        if (v.x * v.x + v.y * v.y < radius * radius)
                        {
                            var t = new Terminal(v + offset, flow);
                            l.Add(t);
                        }
                    }
                }
            }
            return l.ToArray();
        }

        /// <summary>
        /// Randomly generates points on a spherical shell by normalizing a Gaussian.
        /// </summary>
        /// <param name="radius">The sphere radius.</param>
        /// <param name="flow">The terminal flow rate.</param>
        /// <param name="total">The total number of points to generate.</param>
        /// <param name="seed">The seed. If <see cref="int.MinValue"/>, uses default constructor.</param>
        /// <returns></returns>
        public static Terminal[] RandomSphericalShell(double radius, double flow, int total, int seed = int.MinValue)
        {
            var t = new Terminal[total];
            var g = seed != int.MinValue ? new GaussianRandom(seed) : new GaussianRandom();
            for (var i = 0; i < total; ++i)
            {
                t[i] = new Terminal(g.NextVector3().Normalize() * radius, flow);
            }
            return t;
        }

        /// <summary>
        /// Attempts to generate equally spaced terminals on slices of a sphere. Always generates a single point at nadir and zenith.
        /// </summary>
        /// <param name="radius">The sphere radius.</param>
        /// <param name="flow">The terminal flow rate.</param>
        /// <param name="divisions">The number of slices of the sphere.</param>
        /// <returns></returns>
        public static Terminal[] SlicedSphericalShell(double radius, double flow, int divisions)
        {
            var angle = Math.PI / divisions;
            var terms = new List<Terminal>(divisions * divisions)
            {
                new Terminal(new Vector3(0, 0, -radius), flow),
                new Terminal(new Vector3(0, 0, radius), flow)
            };
            for (var ring = 1; ring < divisions; ++ring)
            {
                var azim = angle * ring - Math.PI * 0.5;
                var elems = Math.Floor(2 * divisions * Math.Cos(azim));
                for (var elem = 0; elem < elems; ++elem)
                {
                    var bear = 2 * Math.PI / elems;
                    terms.Add(new Terminal(
                        radius * new Vector3(
                            Math.Cos(azim) * Math.Cos(bear),
                            Math.Cos(azim) * Math.Sin(bear),
                            Math.Sin(azim)),
                        flow));
                }
            }
            return terms.ToArray();
        }

        /// <summary>
        /// Generates random points in a solid angle shell using an inverse CDF method.
        /// </summary>
        /// <param name="radius">The shell radius.</param>
        /// <param name="flow">The terminal flow rate.</param>
        /// <param name="count">The number of terminals to generate.</param>
        /// <param name="direction">The shell direction.</param>
        /// <param name="angle">The shell angle.</param>
        /// <param name="offset">Offset applied to each terminal.</param>
        /// <param name="seed">Random seed. If <see cref="int.MinValue"/>, uses default constructor.</param>
        /// <returns></returns>
        public static Terminal[] SolidAngleShell(double radius, double flow, int count, Vector3 direction,
            double angle, Vector3 offset, int seed = int.MinValue)
        {
            // Define angle from directional axis as (a), and angle around axis as (b)
            // We must make arbitrary choice of the direction we start rotating (a) into, and then (b)
            // For uniform probability over the surface, we must draw angles (a,b) such that these angles are weighted by the area they represent
            // (a,b) ~ dA(a,b) , where dA(a,b) = r^2 sin(a) da db
            // Therefore draw b ~ U[0, 2pi], p(a) = C sin(a) in [0, a_max] (using inverse CDF method) (C is normalising)
            // Total weighting of sin(a) in [0, a_max] = 1 - cos(a_max), draw u ~ U[0, 1 - cos(a_max)], then a = acos(1 - u)
            // Start rotation from d, around t: d -> d cos(a) + n sin(a)
            // Continue around d: n -> n cos(b) + t sin(b)
            // Overall, output direction: d -> d cos(a) + n sin(a) cos(b) + t sin(a) sin(b)

            var dir = direction.Normalize();
            var nor = new CubeGrayCode().GenerateArbitraryNormal(dir);
            var tan = (dir ^ nor).Normalize();

            var terms = new Terminal[count];
            var rand = seed == int.MinValue ? new Random() : new Random(seed);
            var umax = 1.0 - Math.Cos(angle);
            var bmax = Math.PI * 2.0;
            for (var i = 0; i < terms.Length; ++i)
            {
                var u = rand.NextDouble() * umax;
                var a = Math.Acos(1.0 - u);
                var b = rand.NextDouble() * bmax;
                var x = dir * Math.Cos(a)
                    + nor * (Math.Sin(a) * Math.Cos(b))
                    + tan * (Math.Sin(a) * Math.Sin(b));
                terms[i] = new Terminal(offset + radius * x, flow);
            }
            return terms;
        }

        /// <summary>
        /// A ring of terminals.
        /// </summary>
        /// <param name="radius">The circle radius.</param>
        /// <param name="flow">The terminal flow rate.</param>
        /// <param name="count">The number of terminals.</param>
        /// <param name="direction">Normal to the plane in which the terminals lie.</param>
        /// <param name="offset">Offset applied to each.</param>
        /// <returns></returns>
        public static Terminal[] Ring(double radius, double flow, int count, Vector3 direction, Vector3 offset)
        {
            var da = Math.PI * 2.0 / count;
            var t = new Terminal[count];
            var d = direction.Normalize();
            var u = new CubeGrayCode().GenerateArbitraryNormal(d);
            var v = (d ^ u).Normalize();
            var ru = radius * u;
            var rv = radius * v;
            for (var i = 0; i < count; ++i)
            {
                var a = i * da;
                t[i] = new Terminal(ru * Math.Cos(a) + rv * Math.Sin(a) + offset, flow);
            }
            return t;
        }

        /// <summary>
        /// Fills a cone with points on a cubic lattice.
        /// </summary>
        /// <param name="r0">The base radius.</param>
        /// <param name="r1">The radius at height <paramref name="h"/>.</param>
        /// <param name="h">The total height.</param>
        /// <param name="Q">The flow rate at each terminal.</param>
        /// <param name="s">The terminal spacing.</param>
        /// <returns></returns>
        public static Terminal[] Cone(double r0, double r1, double h, double Q, double s)
        {
            var l = new List<Terminal>();
            var w = (int)Math.Floor(Math.Max(r0, r1) / s);
            var d = (int)Math.Floor(h / s);
            var v = (h - d * s) / 2;
            for (var x = -w; x <= w; ++x)
            {
                for (var y = -w; y <= w; ++y)
                {
                    for (var z = 0; z <= d; ++z)
                    {
                        var p = new Vector3(x * s, y * s, z * s + v);
                        var r = r0 + (r1 - r0) / h * p.z;
                        if (p.x * p.x + p.y * p.y < r * r)
                        {
                            l.Add(new Terminal(p, Q));
                        }
                    }
                }
            }
            return l.ToArray();
        }

        /// <summary>
        /// Generates terminals for all lattice sites that are considered inside <paramref name="surface"/>,
        /// using <see cref="SurfaceExtensions.IsPointInside(IAxialBoundsQueryable{TriangleSurfaceTest}, Vector3, Vector3[], int)"/>.
        /// The test directions are generated from <paramref name="lattice"/> using <see cref="VoronoiCell.Connections"/>, and the
        /// minimum hit count is generated from <paramref name="maxMiss"/> and the number of connection directions tested.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="lattice"></param>
        /// <param name="flow"></param>
        /// <param name="maxMiss"></param>
        /// <returns></returns>
        public static Terminal[] LatticeInSurface(IAxialBoundsQueryable<TriangleSurfaceTest> surface,
            Lattice lattice, double flow, int maxMiss = 1)
        {
            static IEnumerable<Vector3> getPoints(TriangleSurfaceTest tst)
            {
                yield return tst.A;
                yield return tst.B;
                yield return tst.C;
            }
            var basisBounds = surface
                .SelectMany(getPoints)
                .Select(lattice.ToBasis)
                .GetTotalBounds();
            var (iMin, jMin, kMin) = basisBounds.Lower.Floor;
            var (iMax, jMax, kMax) = basisBounds.Upper.Ceiling;
            var T = new List<Terminal>((int)Math.Ceiling(surface.Volume() / lattice.Determinant));
            var R = surface.GetAxialBounds().Range.Max * Math.Sqrt(3);
            var D = lattice.VoronoiCell.Connections
                .Select(v => v.Normalize() * R)
                .ToArray();
            for (var i = iMin; i <= iMax; ++i)
            {
                for (var j = jMin; j <= jMax; ++j)
                {
                    for (var k = kMin; k <= kMax; ++k)
                    {
                        var x = lattice.ToSpace(new Vector3(i, j, k));
                        if (surface.IsPointInside(x, D, D.Length - maxMiss))
                        {
                            T.Add(new Terminal(x, flow));
                        }
                    }
                }
            }
            return T.ToArray();
        }
    }
}
