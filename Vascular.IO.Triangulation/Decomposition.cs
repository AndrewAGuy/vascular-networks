using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Triangulation;

namespace Vascular.IO.Triangulation
{
    public class Decomposition
    {
        public static IEnumerable<Mesh> AxialPlanes(Mesh m, int i, int j, int k)
        {
            static double[] getSplit(int n, AxialBounds b, Func<Vector3, double> s)
            {
                var r = new double[n + 2];
                r[0] = double.NegativeInfinity;
                r[n + 1] = double.PositiveInfinity;
                var x = s(b.Lower);
                var y = s(b.Range) / (n + 1);
                for (var m = 1; m <= n; ++m)
                {
                    r[m] = x + m * y;
                }
                return r;
            }

            var T = new AxialBoundsHashTable<Triangle>(m.T);
            var B = T.GetAxialBounds();
            var I = getSplit(i, B, v => v.x);
            var J = getSplit(j, B, v => v.y);
            var K = getSplit(k, B, v => v.z);

            for (var x = 0; x < i + 1; ++x)
            {
                for (var y = 0; y < j + 1; ++y)
                {
                    for (var z = 0; z < k + 1; ++z)
                    {
                        var b = new AxialBounds(
                            new Vector3(I[x], J[y], K[z]),
                            new Vector3(I[x + 1], J[y + 1], K[z + 1]));
                        var M = new Mesh();
                        T.Query(b, t =>
                        {
                            // Check fully above lower planes
                            if (t.A.P.x > I[x] && t.B.P.x > I[x] && t.C.P.x > I[x] &&
                                t.A.P.y > J[y] && t.B.P.y > J[y] && t.C.P.y > J[y] &&
                                t.A.P.z > K[z] && t.B.P.z > K[z] && t.C.P.z > K[z])
                            {
                                M.AddTriangle(t.A.P, t.B.P, t.C.P);
                            }
                        });
                        yield return M;
                    }
                }
            }
        }

        public static Mesh[,,] Octree(Mesh m)
        {
            var B = m.GetAxialBounds();
            var s = (B.Upper + B.Lower) * 0.5;
            var M = new Mesh[2, 2, 2]
            {
                { { new Mesh(), new Mesh() }, { new Mesh(), new Mesh() } },
                { { new Mesh(), new Mesh() }, { new Mesh(), new Mesh() } }
            };
            foreach (var t in m.T)
            {
                var b = t.GetAxialBounds();
                var i = b.Upper.x <= s.x ? 0 : b.Lower.x >= s.x ? 1
                    : s.x - b.Lower.x < b.Upper.x - s.x ? 1 : 0;
                var j = b.Upper.y <= s.y ? 0 : b.Lower.y >= s.y ? 1
                    : s.y - b.Lower.y < b.Upper.y - s.y ? 1 : 0;
                var k = b.Upper.z <= s.z ? 0 : b.Lower.z >= s.z ? 1
                    : s.z - b.Lower.z < b.Upper.z - s.z ? 1 : 0;
                M[i, j, k].AddTriangle(t.A.P, t.B.P, t.C.P);
            }
            return M;
        }

        public static IEnumerable<Mesh> OctreeRecursive(Mesh m, int t)
        {
            var M = new List<Mesh>() { m };
            while (M.Count > 0)
            {
                var N = new List<Mesh>(M.Count * 8);
                foreach (var T in M)
                {
                    if (T.T.Count <= t)
                    {
                        yield return T;
                    }
                    else
                    {
                        var n = Octree(T).Cast<Mesh>().Where(x => x.T.Count != 0);
                        if (n.Count() == 1)
                        {
                            yield return n.First();
                        }
                        else
                        {
                            N.AddRange(n);
                        }
                    }
                }
                M = N;
            }
        }
    }
}
