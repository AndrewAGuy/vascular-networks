using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Topological
{
    /// <summary>
    /// Methods for controlling the number of transients in a branch.
    /// </summary>
    public static class Fragmentation
    {
        /// <summary>
        /// Tries to split each into parts with prescribed slenderness, up to the given limit.
        /// If <paramref name="asSegments"/> is true, treats each segment separately and does not
        /// modify the overall shape of the branch, otherwise interpolates the computed number of
        /// points uniformly onto the branch centreline.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="slenderness"></param>
        /// <param name="limit"></param>
        /// <param name="asSegments"></param>
        public static void Fragment(Branch branch, double slenderness, int limit, bool asSegments = true)
        {
            if (asSegments)
            {
                var reinit = false;
                foreach (var segment in branch.Segments)
                {
                    reinit |= TrySplit(segment, slenderness, limit);
                }

                if (reinit)
                {
                    Reinitialize(branch);
                }
            }
            else
            {
                TrySplit(branch, slenderness, limit);
            }
        }

        /// <summary>
        /// Remove transients matching <paramref name="predicate"/>, assigning <paramref name="newRadius"/> to the replacement segment.
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="predicate"></param>
        /// <param name="newRadius"></param>
        /// <returns></returns>
        public static bool Defragment(Branch branch, Func<Transient, bool> predicate, Func<Transient, double> newRadius)
        {
            var current = branch.Segments[0].End;
            var reinit = false;

            while (current is Transient transient)
            {
                if (predicate(transient))
                {
                    var radius = newRadius(transient);
                    var segment = Topology.RemoveTransient(transient);
                    segment.Radius = radius;
                    current = segment.End;
                    reinit = true;
                }
                else
                {
                    current = transient.Child.End;
                }
            }

            if (reinit)
            {
                Reinitialize(branch);
            }
            return reinit;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Func<Transient, double> MeanRadius => t => (t.Parent.Radius + t.Child.Radius) * 0.5;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Func<Transient, double> BranchRadius => t => t.Child.Branch.Radius;


        /// <summary>
        /// Removes transients which don't do much in terms of excursions.
        /// </summary>
        /// <param name="deviationRatio"></param>
        /// <param name="captureFactor"></param>
        /// <returns></returns>
        public static Func<Transient, bool> DeviationOrTouching(double deviationRatio, double captureFactor)
        {
            return t =>
            {
                var p = t.Parent.Start.Position;
                var c = t.Child.End.Position;
                var d = c - p;
                var l2 = d.LengthSquared;
                var rT2 = Math.Pow((t.Parent.Radius + t.Child.Radius) * captureFactor, 2);
                if (l2 <= rT2)
                {
                    return true;
                }

                var lf = LinearAlgebra.LineFactor(c, d, t.Position);
                var lx = c + lf * d;
                var p2 = Vector3.DistanceSquared(t.Position, lx);
                return p2 <= deviationRatio * l2;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        /// <param name="probability"></param>
        /// <returns></returns>
        public static Func<Transient, bool> RandomDrop(Random random, double probability)
        {
            return t => random.NextDouble() < probability;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        /// <param name="probability"></param>
        /// <returns></returns>
        public static Func<Transient, bool> RandomDrop(Random random, Func<Transient, double> probability)
        {
            return t => random.NextDouble() < probability(t);
        }

        private static void Reinitialize(Branch branch)
        {
            branch.Reinitialize();
            branch.UpdateLengths();
            branch.UpdateRadii();
        }

        private static bool TrySplit(Segment segment, double slenderness, int limit)
        {
            if (segment.Slenderness >= slenderness)
            {
                var splits = (int)Math.Min(Math.Floor(segment.Slenderness / slenderness), limit);
                var fdelta = 1.0 / (splits + 1.0);
                var positions = new Vector3[splits];
                for (var i = 1; i <= splits; ++i)
                {
                    positions[i - 1] = segment.AtFraction(i * fdelta);
                }

                // Now create chain of transients in branch
                for (var i = 0; i < splits; ++i)
                {
                    var end = segment.End;
                    var child = new Segment();
                    var transient = new Transient()
                    {
                        Parent = segment,
                        Child = child,
                        Position = positions[i]
                    };
                    segment.End = transient;
                    child.Start = transient;
                    child.End = end;
                    end.Parent = child;
                    segment = child; // Keep the chain going, we now refer to the new child as the current branch
                }

                return true;
            }
            return false;
        }

        private static void TrySplit(Branch branch, double slenderness, int limit)
        {
            var length = branch.Length;
            var radius = branch.Radius;
            if (length / radius <= slenderness)
            {
                return;
            }

            var splits = (int)Math.Floor(length / (radius * slenderness));
            splits = Math.Min(splits, limit);

            var positions = branch.AtFractions(
                Enumerable.Range(1, splits).Select(i => i / (splits + 1.0)))
                .ToArray();
            branch.Reset();
            var transients = Topology.InsertTransients(branch.Segments[0], splits);
            for(var i = 0; i < splits; ++i)
            {
                transients[i].Position = positions[i];
            }
            branch.UpdateLengths();
            branch.UpdateRadii();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="br"></param>
        /// <param name="F"></param>
        /// <returns></returns>
        public static IEnumerable<Vector3> AtFractions(this Branch br, IEnumerable<double> F)
        {
            var L = br.Length;
            var l = 0.0;
            var s = br.Segments[0];
            foreach (var f in F)
            {
                var lT = f * L;
                while (true)
                {
                    var lR = lT - l;
                    if (lR <= s.Length)
                    {
                        yield return s.AtFraction(lR / s.Length);
                        break;
                    }
                    s = s.End.Children[0];
                    l += s.Length;
                }
            }
        }
    }
}
