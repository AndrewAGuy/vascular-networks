using System;
using System.Collections.Generic;
using System.Text;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Construction.ACCO.Optimizers
{
    public class GradientDescentOptimizer : IBifurcationOptimizer
    {
        public double TerminationLengthFraction { get; set; } = 0.2;
        public int MaxIterations { get; set; } = 100;
        public double ProbeFraction { get; set; } = 0.01;
        public double StepFraction { get; set; } = 0.1;
        public Func<Source, double> Cost { get; set; } = s => s.Volume;
        public double TerminationCostFraction { get; set; } = 0.0001;

        public void Optimize(Bifurcation node)
        {
            var S = node.Network.Source;
            var p = node.Parent.Start.Position;
            var c0 = node.Children[0].End.Position;
            var c1 = node.Children[1].End.Position;
            // Generate basis
            var b0 = (c0 - p).Normalize();
            var n = (b0 ^ (c1 - p)).Normalize();
            var b1 = n ^ b0;
            // Get smallest allowed distance from vertex
            var l0 = Math.Sqrt(
                Math.Min(Vector3.DistanceSquared(p, c0),
                Math.Min(Vector3.DistanceSquared(p, c1),
                Vector3.DistanceSquared(c0, c1)))) * this.TerminationLengthFraction;
            // Setup loop parameters
            var v0 = this.Cost(S);
            var x0 = node.Position;
            var x = x0;
            var f0 = 0.0;
            var f1 = 0.0;
            for (uint i = 0; i < this.MaxIterations; ++i)
            {
                // Get probing scale, check proximity to vertices
                var lm = Math.Sqrt(
                    Math.Min(Vector3.DistanceSquared(x, p),
                    Math.Min(Vector3.DistanceSquared(x, c0),
                    Vector3.DistanceSquared(x, c1))));
                if (lm <= l0)
                {
                    return;
                }
                var lp = lm * this.ProbeFraction;
                // Get derivative
                // TODO: use gradient approximation
                node.Position = x + b0 * lp;
                node.UpdatePhysicalAndPropagate();
                var v0p = this.Cost(S);
                node.Position = x - b0 * lp;
                node.UpdatePhysicalAndPropagate();
                var v0n = this.Cost(S);
                node.Position = x + b1 * lp;
                node.UpdatePhysicalAndPropagate();
                var v1p = this.Cost(S);
                node.Position= x - b1 * lp;
                node.UpdatePhysicalAndPropagate();
                var v1n = this.Cost(S);
                var lpi = 1.0 / (2.0 * lp);
                var d0 = (v0p - v0n) * lpi;
                var d1 = (v1p - v1n) * lpi;
                // Move small amount in direction against gradient
                f0 -= this.StepFraction * d0;
                f1 -= this.StepFraction * d1;
                x = x0 + f0 * b0 + f1 * b1;
                // Get new volume and check fractional change
                node.Position = x;
                node.UpdatePhysicalAndPropagate();
                var v = this.Cost(S);
                var dv = v0 - v;
                if (Math.Abs(dv / v) < this.TerminationCostFraction)
                {
                    return;
                }
                v0 = v;
            }
        }
    }
}
