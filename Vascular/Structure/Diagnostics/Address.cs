using System;
using System.Collections.Generic;
using System.Linq;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    ///
    /// </summary>
    public static class Address
    {
        /// <summary>
        /// Gets the address of <paramref name="current"/> relative to the upstream
        /// branch, or the root if null. For a full relative address, see
        /// <see cref="Relative(Branch, Branch)"/>. If a branch cannot be found in
        /// the children of the parent, it is assigned a symbol -1.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="upstream"></param>
        /// <returns></returns>
        public static List<int> Get(Branch current, Branch? upstream = null)
        {
            var addr = new List<int>();
            while (current!.Start is not Source
                && current != upstream)
            {
                var s = current.Start;
                var i = Array.IndexOf(s.Downstream, current);
                addr.Add(i);
                current = s.Upstream!;
            }
            addr.Reverse();
            return addr;
        }

        /// <summary>
        /// Gets a relative address from <paramref name="start"/> to <paramref name="end"/>
        /// that can be used with <see cref="Navigate(Branch, List{int})"/>.
        /// Upstream jumps are coded with <see cref="int.MinValue"/> rather than -1,
        /// so that invalid connections can be identified in the downstream section.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static List<int> Relative(Branch start, Branch end)
        {
            var gca = Branch.CommonAncestorSafe(start, end);
            // First go up start -> gca
            var diff = start == gca
                ? 0
                : start.UpstreamTo(gca).Count() + 1;
            var addr = Enumerable.Repeat(int.MinValue, diff).ToList();
            // Now go gca -> end
            addr.AddRange(Get(end, gca));
            return addr;
        }

        /// <summary>
        /// Navigates from a branch using the specified address. Negative values are treated
        /// as a single jump upwards, regardless of value.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Branch Navigate(Branch from, List<int> address)
        {
            for (var i = 0; i < address.Count; i++)
            {
                from = address[i] < 0
                    ? from.Parent!
                    : from.Children[address[i]];
            }
            return from;
        }
    }
}
