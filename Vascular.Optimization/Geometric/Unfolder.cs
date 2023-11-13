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
    }
}
