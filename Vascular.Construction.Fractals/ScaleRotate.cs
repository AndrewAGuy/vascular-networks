using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.Fractals
{
    public static class ScaleRotate
    {
        public static (Terminal a, Terminal b) IterateXY(Terminal t, double a0, double s0, double a1, double s1)
        {
            var pb = t.Upstream;
            var bf = new Bifurcation()
            {
                Position = t.Position,
                Parent = pb.Segments[^1],
                Network = t.Network
            };
            pb.Segments[^1].End = pb.End = bf;

            var pdir = pb.Direction;
            var rmat0 = Matrix3.GivensRotationZ(a0);
            var rmat1 = Matrix3.GivensRotationZ(a1);
            var dir0 = rmat0 * pdir * s0;
            var dir1 = rmat1 * pdir * s1;

            var t0 = new Terminal(bf.Position + dir0, 1) { Network = t.Network };
            var t1 = new Terminal(bf.Position + dir1, 1) { Network = t.Network };
            var seg0 = new Segment()
            {
                Start = bf,
                End = t0
            };
            t0.Parent = seg0;
            bf.Children[0] = seg0;
            var seg1 = new Segment()
            {
                Start = bf,
                End = t1
            };
            t1.Parent = seg1;
            bf.Children[1] = seg1;

            var b0 = new Branch()
            {
                Start = bf,
                End = t0
            };
            b0.Initialize();

            var b1 = new Branch()
            {
                Start = bf,
                End = t1
            };
            b1.Initialize();

            bf.UpdateDownstream();

            return (t0, t1);
        }

        public static (Terminal t0, Terminal t1) Iterate(Terminal t, Func<Branch, Vector3> f0, Func<Branch, Vector3> f1)
        {
            var t0 = new Terminal(f0(t.Upstream), 1) { Network = t.Network };
            var v1 = f1(t.Upstream);
            var vb = t.Position;
            var bf = Topology.CreateBifurcation(t.Upstream.Segments[0], t0);
            bf.Position = vb;
            t.Position = v1;
            return (t0, t);
        }

        public static Func<Branch,Vector3> ScaleRotateXY(double angle, double scale)
        {
            return b => b.End.Position + Matrix3.GivensRotationZ(angle) * b.Direction * scale;
        }

        public static void IterateXYSymmetric(Terminal t, double[] a, double[] s)
        {
            var T = new List<Terminal>() { t };
            for (var i = 0; i < a.Length; ++i)
            {
                var A = a[i];
                var S = s[i];
                T = T.SelectMany(tt =>
                {
                    var (t0, t1) = IterateXY(t, A, S, -A, S);
                    return new Terminal[] { t0, t1 };
                }).ToList();
            }
        }
    }
}
