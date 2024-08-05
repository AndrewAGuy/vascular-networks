using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    ///
    /// </summary>
    public static class Address
    {
        /// <summary>
        /// Gets the address of <paramref name="current"/> relative to the upstream branch
        /// <paramref name="upstream"/>, or the absolute address relative to the root if null.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="upstream"></param>
        /// <returns></returns>
        /// <exception cref="TopologyException">
        /// Any branch is not referenced by its parent or is detached from a tree.
        /// The target branch <paramref name="upstream"/> is not an ancestor of <paramref name="current"/>.
        /// </exception>
        public static List<int> Get(Branch current, Branch? upstream = null)
        {
            var addr = new List<int>();
            while (current.Start is not Source
                && current != upstream)
            {
                var i = current.IndexInParent;
                if (i == -1)
                {
                    throw new TopologyException("Cannot take address of branch not referenced by parent");
                }
                addr.Add(i);

                if (current.Parent is Branch parent)
                {
                    current = parent;
                }
                else
                {
                    throw new TopologyException("Cannot take address of branch detached from root");
                }
            }

            if (upstream is not null &&
                current != upstream)
            {
                throw new TopologyException("Specified upstream branch is not ancestor of starting branch");
            }

            addr.Reverse();
            return addr;
        }

        /// <summary>
        /// Gets the relative path from <paramref name="from"/> to <paramref name="to"/>, such that
        /// <c>to = Navigate(from, Relative(from, to))</c>.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        /// <exception cref="TopologyException">
        /// The branches do not share a common ancestor.
        /// </exception>
        public static List<int> Relative(Branch from, Branch to)
        {
            var gca = Branch.CommonAncestor(from, to);
            // First go up start -> gca
            var diff = from == gca
                ? 0
                : from.UpstreamTo(gca).Count() + 1;
            var addr = new List<int>() { -diff };
            // Now go gca -> end
            addr.AddRange(Get(to, gca));
            return addr;
        }

        /// <summary>
        /// Given an address, apply the nagivation pattern starting from the given branch.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="TopologyException">
        /// The specified number of upstream jumps could not be fulfilled, or a child index was out of bounds.
        /// </exception>
        public static Branch Navigate(Branch from, List<int> address)
        {
            for (var i = 0; i < address.Count; i++)
            {
                from = address[i] < 0
                    ? from.GetNthUpstream(-address[i])
                    : from.GetNthChild(address[i]);
            }
            return from;
        }
    }

    /// <summary>
    /// Serializes addresses to human-readable strings
    /// </summary>
    public class AddressFormatter
    {
        private static bool CouldBeNumber(char c) => char.IsDigit(c) || c == '-';

        /// <summary>
        /// Whether to surround the string with the patterns in <see cref="Open"/>, <see cref="Close"/>
        /// </summary>
        public bool Delimit { get; set; } = true;

        /// <summary>
        /// See <see cref="Delimit"/>
        /// </summary>
        public string Open
        {
            get => open;
            set
            {
                if (string.IsNullOrWhiteSpace(value) ||
                    value.Any(CouldBeNumber))
                {
                    throw new FormattingException();
                }
                open = value;
            }
        }

        /// <summary>
        /// See <see cref="Delimit"/>
        /// </summary>
        public string Close
        {
            get => close;
            set
            {
                if (string.IsNullOrWhiteSpace(value) ||
                    value.Any(CouldBeNumber))
                {
                    throw new FormattingException();
                }
                close = value;
            }
        }

        /// <summary>
        /// The pattern to separate elements with
        /// </summary>
        public string Separator
        {
            get => separator;
            set
            {
                if (string.IsNullOrEmpty(value) ||
                    value.Any(CouldBeNumber))
                {
                    throw new FormattingException();
                }
                separator = value;
            }
        }

        private string open = "[";
        private string close = "]";
        private string separator = ",";

        /// <summary>
        ///
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public string ToString(List<int> address)
        {
            var sb = new StringBuilder();
            if (this.Delimit)
                sb.Append(this.Open);
            for (var i = 0; i < address.Count - 1; ++i)
            {
                sb.Append(i);
                sb.Append(this.Separator);
            }
            if (address.Count != 0)
                sb.Append(address[^0]);
            if (this.Delimit)
                sb.Append(this.Close);
            return sb.ToString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public List<int> FromString(string str)
        {
            if (this.Delimit)
            {
                var a = str.IndexOf(this.Open);
                a = a == -1 ? 0 : a + this.Open.Length;
                var b = str.LastIndexOf(this.Close);
                b = b == -1 ? str.Length : b;
                str = str.Substring(a, b - a);
            }

            const StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
            return str
                .Split(this.Separator, opts)
                .Select(int.Parse)
                .ToList();
        }
    }
}
