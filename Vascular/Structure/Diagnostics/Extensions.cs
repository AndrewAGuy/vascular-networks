using System;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    ///
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="root"></param>
        /// <param name="random"></param>
        /// <param name="weighting"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static Branch? SampleDownstream(this Branch root, Random random,
            Func<Branch, double> weighting, Func<Branch, bool> predicate)
        {
            while (true)
            {
                if (predicate(root))
                {
                    if (root.Children.Length == 0)
                    {
                        return root;
                    }

                    var rootWeight = weighting(root);
                    var totalWeight = rootWeight;
                    foreach (var c in root.Children)
                    {
                        totalWeight += weighting(c);
                    }
                    var threshold = random.NextDouble() * totalWeight;

                    if (threshold <= rootWeight)
                    {
                        return root;
                    }

                    var cumulative = rootWeight;
                    foreach (var c in root.Children)
                    {
                        cumulative += weighting(c);
                        if (threshold <= cumulative)
                        {
                            root = c;
                            break;
                        }
                    }
                }
                else
                {
                    if (root.Children.Length == 0)
                    {
                        return null;
                    }

                    var totalWeight = 0.0;
                    foreach (var c in root.Children)
                    {
                        totalWeight += weighting(c);
                    }
                    var threshold = random.NextDouble() * totalWeight;

                    var cumulative = 0.0;
                    foreach (var c in root.Children)
                    {
                        cumulative += weighting(c);
                        if (threshold <= cumulative)
                        {
                            root = c;
                            break;
                        }
                    }
                }
            }
        }
    }
}
