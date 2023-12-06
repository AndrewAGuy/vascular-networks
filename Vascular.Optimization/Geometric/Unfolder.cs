using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    /// <summary>
    /// Legacy mode unfolder using weighted Laplace smoothing, stepping a specified fraction towards
    /// the mean position each iteration.
    /// </summary>
    public class Unfolder
    {
        /// <summary>
        ///
        /// </summary>
        public Func<Segment, double> Weighting { get; set; } = s => s.Flow;

        /// <summary>
        ///
        /// </summary>
        public double Fraction { get; set; } = 0.25;

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public IDictionary<IMobileNode, Vector3> Perturbations(Network n)
        {
            var P = new Dictionary<IMobileNode, Vector3>(n.Segments.Count());

            foreach (var m in n.MobileNodes)
            {
                P[m] = Perturbation(m);
            }

            return P;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Vector3 Perturbation(IMobileNode node)
        {
            if (node is Transient tr)
            {
                var x = 0.5 * (tr.Parent.Start.Position + tr.Child.End.Position);
                return (x - tr.Position) * this.Fraction;
            }
            else
            {
                var x = WeightedMean(node);
                return (x - node.Position) * this.Fraction;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <param name="recalculate"></param>
        public void Apply(Network n, bool recalculate = true)
        {
            var P = Perturbations(n);
            foreach (var (m, p) in P)
            {
                m.Position += p;
            }

            if (recalculate)
            {
                n.Source.CalculatePhysical();
            }
        }

        private Vector3 WeightedMean(IMobileNode n)
        {
            var w = this.Weighting(n.Parent!);
            var x = n.Parent!.Start.Position * w;
            var W = w;
            foreach (var c in n.Children)
            {
                w = this.Weighting(c);
                x += c.End.Position * w;
                W += w;
            }
            return x / W;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <param name="w"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector3 WeightedPredicatedMean(IMobileNode n, Func<Segment, double> w, Func<INode, bool> p)
        {
            double m;
            var W = 0.0;
            var x = Vector3.ZERO;

            if (p(n.Parent!.Start))
            {
                m = w(n.Parent);
                x = n.Parent.Start.Position * m;
                W = m;
            }

            foreach (var c in n.Children)
            {
                if (p(c.End))
                {
                    m = w(c);
                    x += c.End.Position * m;
                    W += m;
                }
            }

            return x / W;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        /// <param name="weight"></param>
        public static void AverageParentAndChild(BranchNode parent, BranchNode child, Func<Branch, double> weight)
        {
            var wp = weight(parent.Upstream!);
            var wxp = wp * parent.Upstream!.Start.Position;

            var wxs = new Vector3();
            var ws = 0.0;
            foreach (var s in child.Upstream!.Siblings)
            {
                var w = weight(s);
                ws += w;
                wxs += w * s.End.Position;
            }

            var wxc = new Vector3();
            var wc = 0.0;
            foreach (var c in child.Downstream)
            {
                var w = weight(c);
                wc += w;
                wxc += w * c.End.Position;
            }

            var a = weight(child.Upstream!);

            var a0 = ws + wp + a;
            var a1 = wc + a;
            var b0 = wxs + wxp;
            var b1 = wxc;

            // Now solve
            //      a0 * x0 = b0 + a * a1
            //      a1 * x1 = b1 + a * a1
            var x0 = (b1 + a1 / a * b0) / (a0 * a1 / a - a);
            var x1 = (a0 * x0 - b0) / a;

            parent.Position = x0;
            child.Position = x1;
        }
    }
}
