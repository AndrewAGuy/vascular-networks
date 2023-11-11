using System.Collections.Generic;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    /// See <see cref="TerminalPositionComparer"/>, but using <see cref="Terminal.CanonicalPosition"/>
    /// for cases where terminal nodes have been perturbed slightly from their original positions.
    /// </summary>
    public class TerminalCanonicalPositionComparer : IEqualityComparer<Terminal>, IComparer<Terminal>
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(Terminal? x, Terminal? y)
        {
            return x!.CanonicalPosition.CompareTo(y!.CanonicalPosition);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(Terminal? x, Terminal? y)
        {
            return x!.CanonicalPosition.Equals(y!.CanonicalPosition);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(Terminal obj)
        {
            return obj.CanonicalPosition.GetHashCode();
        }
    }
}
