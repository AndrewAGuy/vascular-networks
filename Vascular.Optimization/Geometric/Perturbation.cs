using System;
using Vascular.Geometry;
using Vascular.Geometry.Generators;
using Vascular.Structure;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Geometric
{
    /// <summary>
    /// Moving nodes for the sake of it.
    /// </summary>
    public static class Perturbation
    {
        /// <summary>
        /// Iterates all nodes that pass <paramref name="filter"/>, moving them according to the
        /// result of <paramref name="perturb"/>. Recalculates afterwards.
        /// </summary>
        /// <param name="net"></param>
        /// <param name="perturb"></param>
        /// <param name="filter"></param>
        public static void Perturb(Network net, Func<IMobileNode, Vector3> perturb, Func<IMobileNode, bool> filter)
        {
            foreach (var node in net.MobileNodes)
            {
                if (filter(node))
                {
                    node.Position += perturb(node);
                }
            }
            net.Source.CalculatePhysical();
        }

        /// <summary>
        /// Moves using a generated vector from <paramref name="generator"/> scaled by <paramref name="length"/>,
        /// which takes an aggregated characteristic length from the surrounding segments and returns a perturbation length.
        /// The aggregator used is <see cref="Math.Max(double, double)"/> if <paramref name="max"/> is true,
        /// otherwise <see cref="Math.Min(double, double)"/>.
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="length"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Func<IMobileNode, Vector3> PerturbByLength(IVector3Generator generator, Func<double, double> length, bool max = true)
        {
            Func<double, double, double> agg = max ? Math.Max : Math.Min;
            return node =>
            {
                var lp = node.Parent!.Length;
                foreach (var c in node.Children)
                {
                    lp = agg(lp, c.Length);
                }
                return generator.NextVector3() * length(lp);
            };
        }

        /// <summary>
        /// Perturbs using a vector generated from <paramref name="generator"/> scaled by <paramref name="length"/>,
        /// which returns a perturbation length from the total flow through the node.
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Func<IMobileNode, Vector3> PerturbByFlow(IVector3Generator generator, Func<double, double> length)
        {
            return node => generator.NextVector3() * length(node.Flow());
        }

        /// <summary>
        /// Makes sure that terminals are not too short. Similar to intersection resolution and minimum node perturbations,
        /// but generates an arbitrary normal using <paramref name="generator"/> if the length is shorter than <paramref name="t2"/>.
        /// Ensure that <paramref name="t2"/> is less than <paramref name="minLength"/> for this behaviour to be meaningful.
        /// Distance is scaled by <paramref name="factor"/>, so that nodes can be pushed further than the testing distance for safety.
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="minLength"></param>
        /// <param name="t2"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static Func<IMobileNode, Vector3> PerturbTerminals(IVector3Generator generator, double minLength,
            double t2 = 1e-12, double factor = 1.0)
        {
            var ml2 = Math.Pow(minLength, 2);
            return n =>
            {
                foreach (var c in n.Children)
                {
                    var b = c.Branch;
                    if (b.IsTerminal)
                    {
                        var d = b.End.Position - n.Position;
                        var d2 = d.LengthSquared;
                        if (d2 < minLength)
                        {
                            if (d2 <= t2)
                            {
                                d = generator.NextVector3();
                                d2 = d.LengthSquared;
                            }
                            return d * (minLength * factor / Math.Sqrt(d2));
                        }
                    }
                }
                return Vector3.ZERO;
            };
        }

        /// <summary>
        /// Offsets a network by a factor of <paramref name="offset"/>, determined by <paramref name="amplification"/>.
        /// Defaults to unity weighting if null. For amplification that ensures networks converge at their root points,
        /// use either <see cref="FlowEstimatedRadiusAmplification"/> when only flow rates are set,
        /// <see cref="DepthEstimatedRadiusAmplification"/> when only depths are set, or <see cref="ActualRadiusAmplification"/>
        /// when radii are already set. These modes move terminals less than root vessels, to account for the fact that these
        /// vessels are smaller and do not need to be moved as much to separate them.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="offset"></param>
        /// <param name="amplification"></param>
        /// <param name="moveSource"></param>
        /// <param name="moveTerminals"></param>
        public static void Offset(this Network network, Vector3 offset, Func<INode, double>? amplification = null,
            bool moveSource = false, bool moveTerminals = false)
        {
            amplification ??= node => 1;

            foreach (var node in network.Nodes)
            {
                if (node is IMobileNode mobile)
                {
                    mobile.Position += offset * amplification(mobile);
                }
                else if (node is Terminal terminal && moveTerminals)
                {
                    var newPosition = terminal.Position + offset * amplification(terminal);
                    terminal.SetPosition(newPosition);
                }
                else if (node is Source source && moveSource)
                {
                    var newPosition = source.Position + offset * amplification(source);
                    source.SetPosition(newPosition);
                }
            }
        }

        /// <summary>
        /// Uses the approximation that <c>r ~ kQ^(1/3)</c>, thus the appropriate factor should be <c>(Q/Q_0)^(1/3)</c>.
        /// </summary>
        public static Func<INode, double> FlowEstimatedRadiusAmplification =>
            node => Math.Pow(node.Flow() / node.Network().Root.Flow, 1.0 / 3.0);

        /// <summary>
        /// Uses the approximation that <c>r ~ 2^(-1/3)r_p</c> at each bifurcation, thus the appropriate factor is <c>2^(-d/3)</c>.
        /// </summary>
        public static Func<INode, double> DepthEstimatedRadiusAmplification =>
            node => Math.Pow(0.5, (node.Parent?.Branch.Start.Depth ?? 0) / 3.0);

        /// <summary>
        /// Uses the actual ratio of radii, <c>r/r_0</c>.
        /// </summary>
        public static Func<INode, double> ActualRadiusAmplification =>
            node => node.MaxRadius() / node.Network().Root.Radius;
    }
}
