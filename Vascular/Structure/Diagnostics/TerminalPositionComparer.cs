using System.Collections.Generic;
using Vascular.Structure.Nodes;
using Vascular.Geometry;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    /// Compares terminals by position, forwarding everything to the respective methods of <see cref="Vector3"/>.
    /// </summary>
    public class TerminalPositionComparer : IEqualityComparer<Terminal>, IComparer<Terminal>
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(Terminal? x, Terminal? y)
        {
            return x!.Position.CompareTo(y!.Position);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(Terminal? x, Terminal? y)
        {
            return x!.Position.Equals(y!.Position);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(Terminal obj)
        {
            return obj.Position.GetHashCode();
        }
    }
}
