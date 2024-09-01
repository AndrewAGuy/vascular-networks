using System;
using System.Linq;
using Vascular.Geometry;
using Vascular.Structure.Nodes;
using Vascular.Structure.Nodes.Pinned;

namespace Vascular.Structure
{
    /// <summary>
    ///
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double MaxSegmentProperty(this INode node, Func<Segment, double> value)
        {
            return (node.Parent, node.Children.Length) switch
            {
                (Segment p, 0) => value(p),
                (Segment p, _) => Math.Max(value(p), node.Children.Max(value)),
                (null, 0) => double.NaN,
                (null, _) => node.Children.Max(value)
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double MaxBranchProperty(this BranchNode node, Func<Branch, double> value)
        {
            return (node.Upstream, node.Downstream.Length) switch
            {
                (Branch p, 0) => value(p),
                (Branch p, _) => Math.Max(value(p), node.Downstream.Max(value)),
                (null, 0) => double.NaN,
                (null, _) => node.Downstream.Max(value)
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double MinSegmentProperty(this INode node, Func<Segment, double> value)
        {
            return (node.Parent, node.Children.Length) switch
            {
                (Segment p, 0) => value(p),
                (Segment p, _) => Math.Min(value(p), node.Children.Min(value)),
                (null, 0) => double.NaN,
                (null, _) => node.Children.Min(value)
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double MinBranchProperty(this BranchNode node, Func<Branch, double> value)
        {
            return (node.Upstream, node.Downstream.Length) switch
            {
                (Branch p, 0) => value(p),
                (Branch p, _) => Math.Min(value(p), node.Downstream.Min(value)),
                (null, 0) => double.NaN,
                (null, _) => node.Downstream.Min(value)
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static Network Network(this Segment segment)
        {
            return segment.Branch.Network;
        }

        public static Source Origin(this Segment segment)
        {
            return segment.Branch.Origin;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Network Network(this INode node)
        {
            return node.Parent?.Branch.Network ?? node.Children[0].Branch.Network;
        }

        public static Source Origin(this INode node)
        {
            return node.Parent?.Branch.Origin ?? node.Children[0].Branch.Origin;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static double Flow(this INode node)
        {
            return node.Parent?.Flow ?? node.Children[0].Flow;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static double MaxRadius(this INode node)
        {
            return (node.Parent, node.Children.Length) switch
            {
                (Segment p, 0) => p.Radius,
                (Segment p, _) => Math.Max(p.Radius, node.Children.Max(c => c.Radius)),
                (null, 0) => double.NaN,
                (null, _) => node.Children.Max(c => c.Radius)
            };
        }

        /// <summary>
        /// Moves every node according to <paramref name="transform"/>, but does not recompute
        /// any physical properties.
        /// Note that <see cref="MobileTerminal"/> instances do not have their pinning radii
        /// changed, so if <paramref name="transform"/> is not an isometry such terminals may not
        /// be able to move to their desired location without preprocessing.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="transform"></param>
        public static void Transform(this Network network, Func<Vector3, Vector3> transform)
        {
            foreach (var n in network.Nodes)
            {
                if (n is Terminal t)
                {
                    t.SetPosition(transform(t.Position));
                }
                else if (n is Source s)
                {
                    s.SetPosition(transform(s.Position));
                }

                // Test separately here as we may have a mobile terminal, and we need to move the
                // canonical position before we can move the actual location. No present support for
                // changing the pinning radius, but most transforms should be isometries anyway.
                if (n is IMobileNode m)
                {
                    m.Position = transform(m.Position);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <param name="logical"></param>
        /// <param name="physical"></param>
        /// <param name="radii"></param>
        /// <param name="bounds"></param>
        /// <param name="depth"></param>
        /// <param name="pressure"></param>
        /// <param name="radiiMod"></param>
        /// <param name="boundsPad"></param>
        public static void Set(this Network n,
            bool logical = false, bool physical = false, bool radii = false, bool bounds = false,
            int depth = 0, bool pressure = false,
            Func<Branch, double>? radiiMod = null, double boundsPad = 0)
        {
            n.Roots.Apply(r => r.Set(logical, physical, radii, bounds, depth, pressure, radiiMod, boundsPad));
        }

        /// <summary>
        /// Utility method for recomputing a number of properties. Set arguments to true to
        /// indicate that they have changed or are desired outputs, and intermediate steps
        /// will be calculated.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="logical"></param>
        /// <param name="physical"></param>
        /// <param name="radii"></param>
        /// <param name="bounds"></param>
        /// <param name="depth"></param>
        /// <param name="pressure"></param>
        /// <param name="radiiMod"></param>
        /// <param name="boundsPad"></param>
        public static void Set(this Branch root,
            bool logical = false, bool physical = false, bool radii = false, bool bounds = false,
            int depth = 0, bool pressure = false,
            Func<Branch, double>? radiiMod = null, double boundsPad = 0)
        {
            // Work out the required compute path first. Possible paths:
            // Q -> R*,L -> r -> p -> r^,B
            //      |_______________/
            //      v
            //      d,l
            // Anything in between that gets invalidated but not requested must be recomputed.
            var source = root.Start;

            void chainLogicalPhysical()
            {
                if (logical)
                {
                    root.SetLogical();
                    physical = true;
                }
                if (physical)
                {
                    source.CalculatePhysical();
                    radii = true;
                }
            }

            if (pressure)
            {
                // Pressure must be set before bounds due to modification potential.
                chainLogicalPhysical();
                if (radii)
                {
                    source.PropagateRadiiDownstream();
                }
                source.CalculatePressures();

                // Now go to bounds, possibly modifying.
                if (bounds)
                {
                    if (radiiMod is not null)
                    {
                        source.PropagateRadiiDownstream(radiiMod);
                    }
                    if (boundsPad != 0)
                    {
                        source.GenerateDownstreamBounds(boundsPad);
                    }
                    else
                    {
                        source.GenerateDownstreamBounds();
                    }
                }
            }
            else if (bounds || radii)
            {
                // Lump bounds and radii together, as they both have the same invalidation chain.
                chainLogicalPhysical();

                // Go straight to modification here
                if (radii)
                {
                    if (radiiMod is not null)
                    {
                        source.PropagateRadiiDownstream(radiiMod);
                    }
                    else
                    {
                        source.PropagateRadiiDownstream();
                    }
                }
                if (bounds)
                {
                    if (boundsPad != 0)
                    {
                        source.GenerateDownstreamBounds(boundsPad);
                    }
                    else
                    {
                        source.GenerateDownstreamBounds();
                    }
                }
            }
            else
            {
                if (logical)
                {
                    root.SetLogical();
                }
                if (physical)
                {
                    source.CalculatePhysical();
                }
            }

            // Depths only requires length not reduced resistance, so we trust that this is set if not requested.
            // Do this last so that if actually requested, is definitely valid.
            switch (depth)
            {
                case > 0:
                    source.CalculatePathLengthsAndOrder();
                    break;
                case < 0:
                    source.CalculatePathLengthsAndDepths();
                    break;
            }
        }
    }
}
