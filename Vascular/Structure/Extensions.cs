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
        /// <param name="segment"></param>
        /// <returns></returns>
        public static Network Network(this Segment segment)
        {
            return segment.Branch.Network;
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
        /// Utility method for recomputing a number of properties. Set arguments to true to
        /// indicate that they have changed or are desired outputs, and intermediate steps
        /// will be calculated.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="logical"></param>
        /// <param name="physical"></param>
        /// <param name="radii"></param>
        /// <param name="bounds"></param>
        /// <param name="depth"></param>
        /// <param name="pressure"></param>
        /// <param name="radiiMod"></param>
        /// <param name="boundsPad"></param>
        public static void Set(this Network network,
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

            void chainLogicalPhysical()
            {
                if (logical)
                {
                    network.Root.SetLogical();
                    physical = true;
                }
                if (physical)
                {
                    network.Source.CalculatePhysical();
                    radii = true;
                }
            }

            if (pressure)
            {
                // Pressure must be set before bounds due to modification potential.
                chainLogicalPhysical();
                if (radii)
                {
                    network.Source.PropagateRadiiDownstream();
                }
                network.Source.CalculatePressures();

                // Now go to bounds, possibly modifying.
                if (bounds)
                {
                    if (radiiMod is not null)
                    {
                        network.Source.PropagateRadiiDownstream(radiiMod);
                    }
                    if (boundsPad != 0)
                    {
                        network.Source.GenerateDownstreamBounds(boundsPad);
                    }
                    else
                    {
                        network.Source.GenerateDownstreamBounds();
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
                        network.Source.PropagateRadiiDownstream(radiiMod);
                    }
                    else
                    {
                        network.Source.PropagateRadiiDownstream();
                    }
                }
                if (bounds)
                {
                    if (boundsPad != 0)
                    {
                        network.Source.GenerateDownstreamBounds(boundsPad);
                    }
                    else
                    {
                        network.Source.GenerateDownstreamBounds();
                    }
                }
            }
            else
            {
                if (logical)
                {
                    network.Root.SetLogical();
                }
                if (physical)
                {
                    network.Source.CalculatePhysical();
                }
            }

            // Depths only requires length not reduced resistance, so we trust that this is set if not requested.
            // Do this last so that if actually requested, is definitely valid.
            switch (depth)
            {
                case > 0:
                    network.Source.CalculatePathLengthsAndOrder();
                    break;
                case < 0:
                    network.Source.CalculatePathLengthsAndDepths();
                    break;
            }
        }
    }
}
