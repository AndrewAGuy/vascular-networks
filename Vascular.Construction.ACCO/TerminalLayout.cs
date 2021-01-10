using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vascular.Geometry;
using Vascular.Geometry.Generators;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO
{
    public static class TerminalLayout
    {
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
                var azim = (angle * ring) - (Math.PI * 0.5);
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

        public static Terminal[] SolidAngleShell(double radius, double flow, int count, Vector3 direction, double angle, Vector3 offset)
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
            var rand = new Random();
            var umax = 1.0 - Math.Cos(angle);
            var bmax = Math.PI * 2.0;
            for (var i = 0; i < terms.Length; ++i)
            {
                var u = rand.NextDouble() * umax;
                var a = Math.Acos(1.0 - u);
                var b = rand.NextDouble() * bmax;
                var x = (dir * Math.Cos(a))
                    + (nor * (Math.Sin(a) * Math.Cos(b)))
                    + (tan * (Math.Sin(a) * Math.Sin(b)));
                terms[i] = new Terminal(offset + (radius * x), flow);
            }
            return terms;
        }

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
    }
}
