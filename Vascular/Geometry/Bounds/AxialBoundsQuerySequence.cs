using System;
using System.Collections;
using System.Collections.Generic;

namespace Vascular.Geometry.Bounds
{
    /// <summary>
    /// Wraps a sequence of boundable objects, which may be mutated outside of this.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AxialBoundsQuerySequence<T> : IAxialBoundsQueryable<T> where T : IAxialBoundable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sequence"></param>
        public AxialBoundsQuerySequence(IEnumerable<T> sequence)
        {
            this.sequence = sequence ?? Array.Empty<T>();
        }

        private readonly IEnumerable<T> sequence;

        /// <inheritdoc/>
        public void Query(AxialBounds query, Action<T> action)
        {
            foreach (var obj in sequence)
            {
                if (query.Intersects(obj.GetAxialBounds()))
                {
                    action(obj);
                }
            }
        }

        /// <inheritdoc/>
        public AxialBounds GetAxialBounds()
        {
            return sequence.GetTotalBounds();
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return sequence.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return sequence.GetEnumerator();
        }
    }
}
