using System;
using System.Collections.Generic;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// An object that contains multiple objects that can be queried by bounds.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAxialBoundsQueryable<T> : IAxialBoundable, IEnumerable<T> where T : IAxialBoundable
    {
        /// <summary>
        /// Get all objects matching <paramref name="query"/>, and invoke <paramref name="action"/>.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        void Query(AxialBounds query, Action<T> action);
    }
}
