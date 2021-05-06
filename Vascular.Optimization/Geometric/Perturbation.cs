﻿using System;
using Vascular.Geometry;
using Vascular.Geometry.Generators;
using Vascular.Structure;

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
                var lp = node.Parent.Length;
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
            return node => generator.NextVector3() * length(node.Parent.Flow);
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
    }
}