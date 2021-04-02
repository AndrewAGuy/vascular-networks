﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vascular.Geometry;
using Vascular.Structure;
using Vascular.Structure.Actions;
using Vascular.Structure.Nodes;

namespace Vascular.Optimization.Topological
{
    public static class Fragmentation
    {
        public static void Fragment(Branch branch, double slenderness, int limit)
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

        public static void Defragment(Branch branch, Predicate<Transient> predicate, Func<Transient, double> newRadius)
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
        }

        public static Func<Transient, double> MeanRadius()
        {
            return t => (t.Parent.Radius + t.Child.Radius) * 0.5;
        }

        public static Func<Transient, double> BranchRadius()
        {
            return t => t.Child.Branch.Radius;
        }

        public static Predicate<Transient> DeviationOrTouching(double deviationRatio, double captureFactor)
        {
            return t =>
            {
                var p = t.Parent.Start.Position;
                var c = t.Child.End.Position;
                var d = c - p;
                var l2 = d.LengthSquared;
                var rT2 = Math.Pow((t.Parent.Radius + t.Child.Radius) * captureFactor, 2);
                if (l2 < rT2)
                {
                    return true;
                }

                var lf = LinearAlgebra.LineFactor(c, d, t.Position);
                var lx = c + lf * d;
                var p2 = Vector3.DistanceSquared(t.Position, lx);
                return p2 > deviationRatio * l2;
            };
        }

        public static Predicate<Transient> RandomDrop(Random random, double probability)
        {
            return t => random.NextDouble() < probability;
        }

        public static Predicate<Transient> RandomDrop(Random random, Func<Transient, double> probability)
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
    }
}